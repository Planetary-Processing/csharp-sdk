using Planetary;


var sdk = new SDK(7922);
sdk.Connect("1", "1");
sdk.Join();
while (sdk.IsConnected()) {
    for (int i = 0; i < 5; i++) {
        //sdk.Update();
        Thread.Sleep(1000/5);        
    }
    sdk.Message(new Dictionary<string, dynamic>());
    Console.WriteLine(sdk.entities.Count);
}
