using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Google.Protobuf;

namespace Planetary {
  public class SDK {
    public SDK(ulong gameid, string token) {
      var login = new Login {
        Token = token,
        GameID = gameid
      };
      try {
        TcpClient socket = new TcpClient();
        socket.Connect("planetaryprocessing.io", 42);
        NetworkStream stream = socket.GetStream();
        Byte[] dat = encodeLogin(login);
        stream.Write(dat, 0, dat.Length);
        StreamReader sr = new StreamReader(stream, Encoding.UTF8);
        string line = sr.ReadLine();
        Console.WriteLine(decodeLogin(line));
        while (true) {
          while ((line = sr.ReadLine()) != null) {
            Console.WriteLine(decodePacket(line));
          }
        }
      } catch (Exception e) {
        Console.WriteLine(e.ToString());
      }
    }
    public static Login decodeLogin(string s) {
      Byte[] bts = System.Convert.FromBase64String(s);
      Login pckt = Login.Parser.ParseFrom(bts);
      return pckt;
    }
    public static Packet decodePacket(string s) {
      Byte[] bts = System.Convert.FromBase64String(s);
      Packet pckt = Packet.Parser.ParseFrom(bts);
      return pckt;
    }
    public static Byte[] encodeLogin(Login l) {
      return System.Text.Encoding.UTF8.GetBytes(
        System.Convert.ToBase64String(l.ToByteArray()) + "\n");
    }
    public static Byte[] encodePacket(Packet p) {
      return System.Text.Encoding.UTF8.GetBytes(
        System.Convert.ToBase64String(p.ToByteArray()) + "\n");
    }
  }
}
