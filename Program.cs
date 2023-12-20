using Planetary;

StreamReader r = new StreamReader(Environment.GetEnvironmentVariable("HOME") + "/.go-easyops/tokens/user_token");
var token = r.ReadLine();

var sdk = new SDK(1, token, s => Console.WriteLine(s));

while (true) {
  Thread.Sleep(1000/60);
  sdk.Update();
}
