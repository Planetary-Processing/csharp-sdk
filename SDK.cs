using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Google.Protobuf;
using System.Threading;
using System.Threading.Channels;

namespace Planetary {
  public class Entity {
    public double x { get; internal set; }
    public double y { get; internal set; }
    public double z { get; internal set; }
    public Byte[] data { get; internal set; }
    public string type { get; internal set; }
  }

  public class SDK {

    private string UUID;
    private NetworkStream stream = null;
    private StreamReader sr = null;
    private Thread thread;
    private Action<string> onEvent;
    private Channel<Packet> channel = Channel.CreateUnbounded<Packet>();
    private Mutex m = new Mutex();
    private Dictionary<string, Entity> entities = new Dictionary<string, Entity>();

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
        Console.WriteLine("Joined with UUID: " + UUID);
        thread = new Thread(new ThreadStart(recv));
        thread.Start();
        Thread.Sleep(1000);
        send(new Packet{
          Join = new Position{X=0, Y=0, Z=0}
        });
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
        handlePacket(pckt);
      }
      Console.WriteLine(entities.Count());
    }

    private void handlePacket(Packet packet) {
      if (packet.Update != null) {
        Entity e = null;
        if (entities.TryGetValue(packet.Update.EntityID, out e)) {
          e.x = packet.Update.X;
          e.y = packet.Update.Y;
          e.z = packet.Update.Z;
          e.data = packet.Update.Data.ToByteArray();
          e.type = packet.Update.Type;
        } else {
          entities.Add(packet.Update.EntityID, new Entity{
            x = packet.Update.X,
            y = packet.Update.Y,
            z = packet.Update.Z,
            data = packet.Update.Data.ToByteArray(),
            type = packet.Update.Type
          });
        }
      }
      if (packet.Delete != null) {
        entities.Remove(packet.Delete.EntityID);
      }
    }

    public void Message(Dictionary<String, dynamic> msg) {
      var s = JsonSerializer.Serialize(msg);
      send(new Packet{Arbitrary = s});
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
