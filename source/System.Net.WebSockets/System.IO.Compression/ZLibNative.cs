using System.Runtime.InteropServices;

namespace System.IO.Compression;

internal static class ZLibNative
{
	public enum FlushCode
	{
		NoFlush = 0,
		SyncFlush = 2,
		Finish = 4,
		Block = 5
	}

	public enum ErrorCode
	{
		Ok = 0,
		StreamEnd = 1,
		StreamError = -2,
		DataError = -3,
		MemError = -4,
		BufError = -5,
		VersionError = -6
	}

	public enum CompressionLevel
	{
		NoCompression = 0,
		BestSpeed = 1,
		DefaultCompression = -1,
		BestCompression = 9
	}

	public enum CompressionStrategy
	{
		DefaultStrategy
	}

	public enum CompressionMethod
	{
		Deflated = 8
	}

	public sealed class ZLibStreamHandle : SafeHandle
	{
		public enum State
		{
			NotInitialized,
			InitializedForDeflate,
			InitializedForInflate,
			Disposed
		}

		private ZStream _zStream;

		private volatile State _initializationState;

		public override bool IsInvalid => handle == new IntPtr(-1);

		public State InitializationState => _initializationState;

		public IntPtr NextIn
		{
			set
			{
				_zStream.nextIn = value;
			}
		}

		public uint AvailIn
		{
			get
			{
				return _zStream.availIn;
			}
			set
			{
				_zStream.availIn = value;
			}
		}

		public IntPtr NextOut
		{
			set
			{
				_zStream.nextOut = value;
			}
		}

		public uint AvailOut
		{
			get
			{
				return _zStream.availOut;
			}
			set
			{
				_zStream.availOut = value;
			}
		}

		public ZLibStreamHandle()
			: base(new IntPtr(-1), ownsHandle: true)
		{
			_zStream.Init();
			_initializationState = State.NotInitialized;
			SetHandle(IntPtr.Zero);
		}

		protected override bool ReleaseHandle()
		{
			return InitializationState switch
			{
				State.NotInitialized => true, 
				State.InitializedForDeflate => DeflateEnd() == ErrorCode.Ok, 
				State.InitializedForInflate => InflateEnd() == ErrorCode.Ok, 
				State.Disposed => true, 
				_ => false, 
			};
		}

		private void EnsureNotDisposed()
		{
			if (InitializationState == State.Disposed)
			{
				throw new ObjectDisposedException(GetType().ToString());
			}
		}

		private void EnsureState(State requiredState)
		{
			if (InitializationState != requiredState)
			{
				throw new InvalidOperationException("InitializationState != " + requiredState);
			}
		}

		public unsafe ErrorCode DeflateInit2_(CompressionLevel level, int windowBits, int memLevel, CompressionStrategy strategy)
		{
			EnsureNotDisposed();
			EnsureState(State.NotInitialized);
			fixed (ZStream* stream = &_zStream)
			{
				ErrorCode result = global::Interop.zlib.DeflateInit2_(stream, level, CompressionMethod.Deflated, windowBits, memLevel, strategy);
				_initializationState = State.InitializedForDeflate;
				return result;
			}
		}

		public unsafe ErrorCode Deflate(FlushCode flush)
		{
			EnsureNotDisposed();
			EnsureState(State.InitializedForDeflate);
			fixed (ZStream* stream = &_zStream)
			{
				return global::Interop.zlib.Deflate(stream, flush);
			}
		}

		public unsafe ErrorCode DeflateEnd()
		{
			EnsureNotDisposed();
			EnsureState(State.InitializedForDeflate);
			fixed (ZStream* stream = &_zStream)
			{
				ErrorCode result = global::Interop.zlib.DeflateEnd(stream);
				_initializationState = State.Disposed;
				return result;
			}
		}

		public unsafe ErrorCode InflateInit2_(int windowBits)
		{
			EnsureNotDisposed();
			EnsureState(State.NotInitialized);
			fixed (ZStream* stream = &_zStream)
			{
				ErrorCode result = global::Interop.zlib.InflateInit2_(stream, windowBits);
				_initializationState = State.InitializedForInflate;
				return result;
			}
		}

		public unsafe ErrorCode Inflate(FlushCode flush)
		{
			EnsureNotDisposed();
			EnsureState(State.InitializedForInflate);
			fixed (ZStream* stream = &_zStream)
			{
				return global::Interop.zlib.Inflate(stream, flush);
			}
		}

		public unsafe ErrorCode InflateEnd()
		{
			EnsureNotDisposed();
			EnsureState(State.InitializedForInflate);
			fixed (ZStream* stream = &_zStream)
			{
				ErrorCode result = global::Interop.zlib.InflateEnd(stream);
				_initializationState = State.Disposed;
				return result;
			}
		}
	}

	internal struct ZStream
	{
		internal IntPtr nextIn;

		internal IntPtr nextOut;

		internal IntPtr msg;

		private readonly IntPtr internalState;

		internal uint availIn;

		internal uint availOut;

		internal void Init()
		{
		}
	}

	public static ErrorCode CreateZLibStreamForDeflate(out ZLibStreamHandle zLibStreamHandle, CompressionLevel level, int windowBits, int memLevel, CompressionStrategy strategy)
	{
		zLibStreamHandle = new ZLibStreamHandle();
		return zLibStreamHandle.DeflateInit2_(level, windowBits, memLevel, strategy);
	}

	public static ErrorCode CreateZLibStreamForInflate(out ZLibStreamHandle zLibStreamHandle, int windowBits)
	{
		zLibStreamHandle = new ZLibStreamHandle();
		return zLibStreamHandle.InflateInit2_(windowBits);
	}
}
