using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Google.Protobuf;
using System.Threading;
using System.Threading.Channels;

namespace Planetary {
  public class SDK {

    private string UUID;
    private NetworkStream stream = null;
    private StreamReader sr = null;
    private Thread thread;
    private Action<string> onEvent;
    private Channel<Packet> channel = Channel.CreateUnbounded<Packet>();
    private Mutex m = new Mutex();

    public SDK(ulong gameid, string token, Action<string> callback) {
      onEvent = callback;
      var login = new Login {
        Token = token,
        GameID = gameid
      };
      try {
        TcpClient socket = new TcpClient();
        socket.Connect("planetaryprocessing.io", 42);
        stream = socket.GetStream();
        Byte[] dat = encodeLogin(login);
        stream.Write(dat, 0, dat.Length);
        sr = new StreamReader(stream, Encoding.UTF8);
        string line = sr.ReadLine();
        Login uuid = decodeLogin(line);
        UUID = uuid.UUID;
        Console.WriteLine(UUID);
        thread = new Thread(new ThreadStart(recv));
        thread.Start();
      } catch (Exception e) {
        Console.WriteLine(e.ToString());
        if (sr != null) {
          sr.Dispose();
        }
      }
    }

    public void Update() {
      Packet pckt;
      while (channel.Reader.TryRead(out pckt)) {
        Console.WriteLine(pckt);
      }
    }

    public void Message(Dictionary<String, dynamic> msg) {
      Console.WriteLine(JsonSerializer.Serialize(msg));
    }

    private void send(Packet packet) {
      m.WaitOne();
      try {
        Byte[] bts = encodePacket(packet);
        stream.Write(bts, 0, bts.Length);
      } catch (Exception e) {
        Console.WriteLine(e.ToString());
        if (sr != null) {
          sr.Dispose();
        }
      } finally {
        m.ReleaseMutex();
      }
    }

    private void recv() {
      try {
        while (true) {
          string line;
          while ((line = sr.ReadLine()) != null) {
            if (!channel.Writer.TryWrite(decodePacket(line))) {
              throw new Exception("lol");
            }
          }
        }
      } catch (Exception e) {
        Console.WriteLine(e.ToString());
        if (sr != null) {
          sr.Dispose();
        }
      }
    }

    private static Login decodeLogin(string s) {
      Byte[] bts = System.Convert.FromBase64String(s);
      Login pckt = Login.Parser.ParseFrom(bts);
      return pckt;
    }

    private static Packet decodePacket(string s) {
      Byte[] bts = System.Convert.FromBase64String(s);
      Packet pckt = Packet.Parser.ParseFrom(bts);
      return pckt;
    }

    private static Byte[] encodeLogin(Login l) {
      return Encoding.UTF8.GetBytes(
        System.Convert.ToBase64String(l.ToByteArray()) + "\n");
    }

    private static Byte[] encodePacket(Packet p) {
      return Encoding.UTF8.GetBytes(
        System.Convert.ToBase64String(p.ToByteArray()) + "\n");
    }
  }
}
