namespace AsterNET.FastAGI.Scripts
{
	class AGINoAction : AGIScript
	{
		protected override void Execute(AGIRequest request, AGIChannel channel)
		{
			channel.Hangup();
		}
	}
}
