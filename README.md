# ghostlineAPI
A library you can just add, and it serves your C# objects (with easy Authentication)

Just add some annotations to one of your classes:

	[GhostWrite,GhostRead]
	public static List<ElkDog> UntrainedElkDogs { get; set; }

	[GhostRead]
	public static List<ElkDog> TrainedElkDogs { get; set; }

Then set some server variables:

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
		if (String.IsNullOrEmpty(req.Headers["Authorization"]))
			return false;

		if (req.Headers["Authorization"].Equals("27bc5f2c-bed5-41c7-8a5d-aec966212146"))
			return true;
		return false;
	};




