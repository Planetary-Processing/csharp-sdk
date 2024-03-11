using Planetary;

StreamReader r = new StreamReader(Environment.GetEnvironmentVariable("HOME") + "/.go-easyops/tokens/user_token");
var token = r.ReadLine();

var sdk = new SDK(1);
sdk.Connect("test", "testsdafasdfsda");
while (sdk.IsConnected()) {
    for (int i = 0; i < 5; i++) {
        sdk.Update();
        Thread.Sleep(1000/5);        
    }
    sdk.Message(new Dictionary<string, dynamic>());
    Console.WriteLine(sdk.entities.Count);
}
