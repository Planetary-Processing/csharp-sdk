using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Google.Protobuf;

namespace Planetary {
  public class SDK {
    public SDK() {
      var pckt = new Planetary.Login {
        Token = "",
        GameID = 1
      };
      string init = System.Convert.ToBase64String(pckt.ToByteArray()) + "\n";
      try {
        TcpClient socket = new TcpClient();
        socket.Connect("planetaryprocessing.io", 42);
        NetworkStream stream = socket.GetStream();
        Byte[] dat = System.Text.Encoding.UTF8.GetBytes(init);
        stream.Write(dat, 0, dat.Length);
        StreamReader sr = new StreamReader(stream, Encoding.UTF8);
        string line = sr.ReadLine();
        decodeLogin(line);
        while (true) {
          while ((line = sr.ReadLine()) != null) {
            decodePacket(line);
          }
        }
      } catch (Exception e) {
        Console.WriteLine(e.ToString());
      }
    }
    public static void decodeLogin(string s) {
      Byte[] bts = System.Convert.FromBase64String(s);
      Planetary.Login pckt = Planetary.Login.Parser.ParseFrom(bts);
      Console.WriteLine(pckt);
    }
    public static void decodePacket(string s) {
      Byte[] bts = System.Convert.FromBase64String(s);
      Planetary.Packet pckt = Planetary.Packet.Parser.ParseFrom(bts);
      Console.WriteLine(pckt);
    }
  }
}
