using Planetary;

StreamReader r = new StreamReader(Environment.GetEnvironmentVariable("HOME") + "/.go-easyops/tokens/user_token");
var token = r.ReadLine();

var sdk = new SDK(1);
sdk.Connect(token);
Console.WriteLine("???");
while (sdk.IsConnected()) {
  Thread.Sleep(1000/60);
  sdk.Update();
  Console.WriteLine("---");
}
Console.WriteLine("died");
