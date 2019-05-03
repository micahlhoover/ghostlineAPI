# ghostlineAPI
A super easy auto API library (with easy Authentication). Just add annotations and then start the server.

Step #1: Add some annotations to one of your classes:

	[GhostWrite,GhostRead]
	public static List<ElkDog> UntrainedElkDogs { get; set; }

	[GhostRead]
	public static List<ElkDog> TrainedElkDogs { get; set; }

Step #2: Set some server variables:

	GhostLineAPIServer apiServer = new GhostLineAPIServer
	{
		ParentObj = (object)this,
		Address = "127.0.0.1",
		Port = 19001
	};
	apiServer.SetupAndStartServer();

And all of the sudden you are serving out data and able to receive it.

Authentication can optionally be enabled like this:

	apiServer.Authenticator = delegate (HttpListenerRequest req)
	{
		// You can put any C# code here, so you can compare with user database
		if (String.IsNullOrEmpty(req.Headers["Authorization"]))
			return false;

		if (req.Headers["Authorization"].Equals("27bc5f2c-bed5-41c7-8a5d-aec966212146"))
			return true;
		return false;
	};

And just like that you can customize authentication without any changes to the server code.

Item retrievable with open query patterns is available.

So this URL:

	http://127.0.0.1:19001/UntrainedElkDogs?CanFly=True

Can give you:

	[]

While this:

	http://127.0.0.1:19001/UntrainedElkDogs?CanFly=False

Can give you:

	[{"CanFly":false,"Name":"Furtile","MarketValue":300.0,"Role":0,"Longitude":-78.66111,"Latitude":35.90557,"Id":"66f7d533-28e4-44a2-95f3-8bb88429c80b"},{"CanFly":false,"Name":"Jeb","MarketValue":300.0,"Role":0,"Longitude":-78.66111,"Latitude":35.90557,"Id":"8e687be8-d1db-4b7a-8285-30870b43ef57"},{"CanFly":false,"Name":"Benji","MarketValue":300.0,"Role":0,"Longitude":-78.66111,"Latitude":35.90557,"Id":"ec0669f4-a6f9-4260-98c1-54ebe2c7725c"}]



