using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Google.Protobuf;
using System.Threading;
using System.Threading.Channels;
using RC4Cryptography;

namespace Planetary {
  public class Entity {
    public string id;
    public double x;
    public double y;
    public double z;
    public Byte[] data;
    public string type;
  }

  public class SDK {

    private ulong gameID;
    private bool connected = false;
    public string UUID;
    private NetworkStream stream = null;
    private StreamReader sr = null;
    private Thread thread;
    private Action<Dictionary<string, dynamic>> onEvent;
    private Channel<Packet> channel = Channel.CreateUnbounded<Packet>();
    private Mutex m = new Mutex();
    public readonly Dictionary<string, Entity> entities = new Dictionary<string, Entity>();
    private RC4 inp = new RC4(Encoding.UTF8.GetBytes("guacamole"));
    private RC4 oup = new RC4(Encoding.UTF8.GetBytes("guacamole"));

    public SDK(ulong gameid, string token, Action<Dictionary<string, dynamic>> callback) {
      gameID = gameid;
    }

    public SDK(ulong gameid) {
      gameID = gameid;
    }

    public void Connect(string username, string password) {
      var login = new Login {
        Email = username,
        Password = password,
        GameID = gameID
      };
      UUID = init(login);
    }

    public void Connect(string token) {
      var login = new Login {
        Token = token,
        GameID = gameID
      };
      UUID = init(login);
    }

    private string init(Login login) {
      string uuid = "";
      try {
        IPHostEntry ipHostInfo = Dns.GetHostEntry("localhost");
        IPAddress ipAddress = ipHostInfo.AddressList[0];
        Socket socket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        socket.Connect(ipAddress, 4101);
        stream = new NetworkStream(socket);
        Byte[] dat = encodeLogin(login);
        stream.Write(dat, 0, dat.Length);
        sr = new StreamReader(stream, Encoding.UTF8);
        string line = sr.ReadLine();
        Login resp = decodeLogin(line);
        uuid = resp.UUID;
        thread = new Thread(new ThreadStart(recv));
        thread.Start();
        Thread.Sleep(1000);
        connected = true;
        send(new Packet{
          Join = new Position{X=0, Y=0, Z=0}
        });
      } catch (Exception e) {
        if (sr != null) {
          sr.Dispose();
        }
        connected = false;
        throw e;
      }
      return uuid;
    }

    public void Update() {
      Packet pckt;
      while (channel.Reader.TryRead(out pckt)) {
        handlePacket(pckt);
      }
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
            id = packet.Update.EntityID,
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

    public bool IsConnected() {
      return connected;
    }

    public void Message(Dictionary<String, dynamic> msg) {
      var s = JsonSerializer.Serialize(msg);
      send(new Packet{Arbitrary = s});
    }

    private void send(Packet packet) {
      if ( connected == false ) {
         throw new Exception("send called before connection is established");
      }
      m.WaitOne();
      // perhaps an automatic re-init would be useful here
      try {
        Byte[] bts = encodePacket(packet);
        stream.Write(bts, 0, bts.Length);
      } catch (Exception e) {
        if (sr != null) {
          sr.Dispose();
        }
        connected = false;
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
              throw new Exception("failed to read packet");
            }
          }
        }
      } catch (Exception e) {
        if (sr != null) {
          sr.Dispose();
        }
        connected = false;
      }
    }

    private Login decodeLogin(string s) {
      Byte[] bts = System.Convert.FromBase64String(s);
      Login pckt = Login.Parser.ParseFrom(bts);
      return pckt;
    }

    private Packet decodePacket(string s) {
      Byte[] bts = System.Convert.FromBase64String(s);
      bts = inp.Apply(bts);
      Packet pckt = Packet.Parser.ParseFrom(bts);
      return pckt;
    }

    private Byte[] encodeLogin(Login l) {
      return Encoding.UTF8.GetBytes(
        System.Convert.ToBase64String(l.ToByteArray()) + "\n");
    }

    private Byte[] encodePacket(Packet p) {
      return Encoding.UTF8.GetBytes(
        System.Convert.ToBase64String(oup.Apply(p.ToByteArray())) + "\n");
    }

    private Dictionary<String, dynamic> decodeEvent(string e) {
      return JsonSerializer.Deserialize<Dictionary<String, dynamic>>(e);
    }
  }
}
