StreamReader r = new StreamReader(Environment.GetEnvironmentVariable("HOME") + "/.go-easyops/tokens/user_token");
var token = r.ReadLine();

var sdk = new Planetary.SDK(1, token, s => Console.WriteLine(s));
Console.ReadKey();
