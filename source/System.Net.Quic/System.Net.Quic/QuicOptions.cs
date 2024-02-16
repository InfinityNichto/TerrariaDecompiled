namespace System.Net.Quic;

public class QuicOptions
{
	public int MaxBidirectionalStreams { get; set; } = 100;


	public int MaxUnidirectionalStreams { get; set; } = 100;


	public TimeSpan IdleTimeout { get; set; }
}
