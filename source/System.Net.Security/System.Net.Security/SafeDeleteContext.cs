using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Authentication.ExtendedProtection;

namespace System.Net.Security;

internal abstract class SafeDeleteContext : SafeHandle
{
	internal global::Interop.SspiCli.CredHandle _handle;

	private static readonly IdnMapping s_idnMapping = new IdnMapping();

	protected SafeFreeCredentials _EffectiveCredential;

	public override bool IsInvalid
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			if (!base.IsClosed)
			{
				return _handle.IsZero;
			}
			return true;
		}
	}

	protected SafeDeleteContext()
		: base(IntPtr.Zero, ownsHandle: true)
	{
		_handle = default(global::Interop.SspiCli.CredHandle);
	}

	public override string ToString()
	{
		return _handle.ToString();
	}

	internal unsafe static int InitializeSecurityContext(ref SafeFreeCredentials inCredentials, ref SafeDeleteSslContext refContext, string targetName, global::Interop.SspiCli.ContextFlags inFlags, global::Interop.SspiCli.Endianness endianness, InputSecurityBuffers inSecBuffers, ref SecurityBuffer outSecBuffer, ref global::Interop.SspiCli.ContextFlags outFlags)
	{
		if (inCredentials == null)
		{
			throw new ArgumentNullException("inCredentials");
		}
		global::Interop.SspiCli.SecBufferDesc secBufferDesc = new global::Interop.SspiCli.SecBufferDesc(inSecBuffers.Count);
		global::Interop.SspiCli.SecBufferDesc outputBuffer = new global::Interop.SspiCli.SecBufferDesc(1);
		bool flag = (((inFlags & global::Interop.SspiCli.ContextFlags.AllocateMemory) != 0) ? true : false);
		int result = -1;
		bool flag2 = true;
		if (refContext != null)
		{
			flag2 = refContext._handle.IsZero;
		}
		IntPtr intPtr = IntPtr.Zero;
		try
		{
			Span<global::Interop.SspiCli.SecBuffer> span = stackalloc global::Interop.SspiCli.SecBuffer[3];
			fixed (global::Interop.SspiCli.SecBuffer* ptr = span)
			{
				void* pBuffers = ptr;
				fixed (byte* ptr2 = inSecBuffers._item0.Token)
				{
					void* ptr3 = ptr2;
					fixed (byte* ptr4 = inSecBuffers._item1.Token)
					{
						void* ptr5 = ptr4;
						fixed (byte* ptr6 = inSecBuffers._item2.Token)
						{
							void* ptr7 = ptr6;
							secBufferDesc.pBuffers = pBuffers;
							if (inSecBuffers.Count > 2)
							{
								span[2].BufferType = inSecBuffers._item2.Type;
								if (inSecBuffers._item2.UnmanagedToken != null)
								{
									span[2].pvBuffer = inSecBuffers._item2.UnmanagedToken.DangerousGetHandle();
									span[2].cbBuffer = ((ChannelBinding)inSecBuffers._item2.UnmanagedToken).Size;
								}
								else
								{
									span[2].cbBuffer = inSecBuffers._item2.Token.Length;
									span[2].pvBuffer = (IntPtr)ptr7;
								}
							}
							if (inSecBuffers.Count > 1)
							{
								span[1].BufferType = inSecBuffers._item1.Type;
								if (inSecBuffers._item1.UnmanagedToken != null)
								{
									span[1].pvBuffer = inSecBuffers._item1.UnmanagedToken.DangerousGetHandle();
									span[1].cbBuffer = ((ChannelBinding)inSecBuffers._item1.UnmanagedToken).Size;
								}
								else
								{
									span[1].cbBuffer = inSecBuffers._item1.Token.Length;
									span[1].pvBuffer = (IntPtr)ptr5;
								}
							}
							if (inSecBuffers.Count > 0)
							{
								span[0].BufferType = inSecBuffers._item0.Type;
								if (inSecBuffers._item0.UnmanagedToken != null)
								{
									span[0].pvBuffer = inSecBuffers._item0.UnmanagedToken.DangerousGetHandle();
									span[0].cbBuffer = ((ChannelBinding)inSecBuffers._item0.UnmanagedToken).Size;
								}
								else
								{
									span[0].cbBuffer = inSecBuffers._item0.Token.Length;
									span[0].pvBuffer = (IntPtr)ptr3;
								}
							}
							fixed (byte* ptr8 = outSecBuffer.token)
							{
								global::Interop.SspiCli.SecBuffer secBuffer = default(global::Interop.SspiCli.SecBuffer);
								outputBuffer.pBuffers = &secBuffer;
								secBuffer.cbBuffer = outSecBuffer.size;
								secBuffer.BufferType = outSecBuffer.type;
								secBuffer.pvBuffer = ((outSecBuffer.token == null || outSecBuffer.token.Length == 0) ? IntPtr.Zero : ((IntPtr)(ptr8 + outSecBuffer.offset)));
								if ((refContext == null || refContext.IsInvalid) && flag2)
								{
									refContext = new SafeDeleteSslContext();
								}
								if (targetName == null || targetName.Length == 0)
								{
									targetName = " ";
								}
								string ascii = s_idnMapping.GetAscii(targetName);
								fixed (char* ptr9 = ascii)
								{
									result = MustRunInitializeSecurityContext(ref inCredentials, flag2, (byte*)(((object)targetName == " ") ? null : ptr9), inFlags, endianness, &secBufferDesc, refContext, ref outputBuffer, ref outFlags, null);
									if (flag)
									{
										intPtr = secBuffer.pvBuffer;
									}
									outSecBuffer.size = secBuffer.cbBuffer;
									outSecBuffer.type = secBuffer.BufferType;
									outSecBuffer.token = ((outSecBuffer.size > 0) ? new Span<byte>((void*)secBuffer.pvBuffer, secBuffer.cbBuffer).ToArray() : null);
									if (inSecBuffers.Count > 1 && span[1].BufferType == SecurityBufferType.SECBUFFER_EXTRA && inSecBuffers._item1.Type == SecurityBufferType.SECBUFFER_EMPTY)
									{
										int cbBuffer = span[1].cbBuffer;
										int num = inSecBuffers._item0.Token.Length - span[1].cbBuffer;
										span[0].cbBuffer = cbBuffer;
										span[0].pvBuffer = span[0].pvBuffer + num;
										span[1].BufferType = SecurityBufferType.SECBUFFER_EMPTY;
										span[1].cbBuffer = 0;
										secBuffer.cbBuffer = 0;
										if (intPtr != IntPtr.Zero)
										{
											global::Interop.SspiCli.FreeContextBuffer(intPtr);
											intPtr = IntPtr.Zero;
										}
										result = MustRunInitializeSecurityContext(ref inCredentials, flag2, (byte*)(((object)targetName == " ") ? null : ptr9), inFlags, endianness, &secBufferDesc, refContext, ref outputBuffer, ref outFlags, null);
										if (flag)
										{
											intPtr = secBuffer.pvBuffer;
										}
										if (secBuffer.cbBuffer > 0)
										{
											if (outSecBuffer.size == 0)
											{
												outSecBuffer.size = secBuffer.cbBuffer;
												outSecBuffer.type = secBuffer.BufferType;
												outSecBuffer.token = new Span<byte>((void*)secBuffer.pvBuffer, secBuffer.cbBuffer).ToArray();
											}
											else
											{
												byte[] array = new byte[outSecBuffer.size + secBuffer.cbBuffer];
												Buffer.BlockCopy(outSecBuffer.token, 0, array, 0, outSecBuffer.size);
												new Span<byte>((void*)secBuffer.pvBuffer, secBuffer.cbBuffer).CopyTo(new Span<byte>(array, outSecBuffer.size, secBuffer.cbBuffer));
												outSecBuffer.size = array.Length;
												outSecBuffer.token = array;
											}
										}
										if (span[1].BufferType == SecurityBufferType.SECBUFFER_EXTRA)
										{
											result = -2146893032;
										}
									}
								}
							}
						}
					}
				}
			}
		}
		finally
		{
			if (intPtr != IntPtr.Zero)
			{
				global::Interop.SspiCli.FreeContextBuffer(intPtr);
			}
		}
		return result;
	}

	private unsafe static int MustRunInitializeSecurityContext(ref SafeFreeCredentials inCredentials, bool isContextAbsent, byte* targetName, global::Interop.SspiCli.ContextFlags inFlags, global::Interop.SspiCli.Endianness endianness, global::Interop.SspiCli.SecBufferDesc* inputBuffer, SafeDeleteContext outContext, ref global::Interop.SspiCli.SecBufferDesc outputBuffer, ref global::Interop.SspiCli.ContextFlags attributes, SafeFreeContextBuffer handleTemplate)
	{
		int num = -2146893055;
		try
		{
			bool success = false;
			inCredentials.DangerousAddRef(ref success);
			outContext.DangerousAddRef(ref success);
			global::Interop.SspiCli.CredHandle credentialHandle = inCredentials._handle;
			global::Interop.SspiCli.CredHandle credHandle = outContext._handle;
			void* ptr = (credHandle.IsZero ? null : (&credHandle));
			isContextAbsent = ptr == null;
			num = global::Interop.SspiCli.InitializeSecurityContextW(ref credentialHandle, ptr, targetName, inFlags, 0, endianness, inputBuffer, 0, ref outContext._handle, ref outputBuffer, ref attributes, out var _);
		}
		finally
		{
			if (outContext._EffectiveCredential != inCredentials && (num & 0x80000000u) == 0L)
			{
				outContext._EffectiveCredential?.DangerousRelease();
				outContext._EffectiveCredential = inCredentials;
			}
			else
			{
				inCredentials.DangerousRelease();
			}
			outContext.DangerousRelease();
		}
		if (handleTemplate != null)
		{
			handleTemplate.Set(((global::Interop.SspiCli.SecBuffer*)outputBuffer.pBuffers)->pvBuffer);
			if (handleTemplate.IsInvalid)
			{
				handleTemplate.SetHandleAsInvalid();
			}
		}
		if (isContextAbsent && (num & 0x80000000u) != 0L)
		{
			outContext._handle.SetToInvalid();
		}
		return num;
	}

	internal unsafe static int AcceptSecurityContext(ref SafeFreeCredentials inCredentials, ref SafeDeleteSslContext refContext, global::Interop.SspiCli.ContextFlags inFlags, global::Interop.SspiCli.Endianness endianness, InputSecurityBuffers inSecBuffers, ref SecurityBuffer outSecBuffer, ref global::Interop.SspiCli.ContextFlags outFlags)
	{
		if (inCredentials == null)
		{
			throw new ArgumentNullException("inCredentials");
		}
		global::Interop.SspiCli.SecBufferDesc secBufferDesc = new global::Interop.SspiCli.SecBufferDesc(inSecBuffers.Count);
		global::Interop.SspiCli.SecBufferDesc outputBuffer = new global::Interop.SspiCli.SecBufferDesc(2);
		bool flag = (((inFlags & global::Interop.SspiCli.ContextFlags.AllocateMemory) != 0) ? true : false);
		int result = -1;
		bool flag2 = true;
		if (refContext != null)
		{
			flag2 = refContext._handle.IsZero;
		}
		Span<global::Interop.SspiCli.SecBuffer> span = stackalloc global::Interop.SspiCli.SecBuffer[2];
		span[1].pvBuffer = IntPtr.Zero;
		try
		{
			Span<global::Interop.SspiCli.SecBuffer> span2 = stackalloc global::Interop.SspiCli.SecBuffer[3];
			fixed (global::Interop.SspiCli.SecBuffer* ptr = span2)
			{
				void* pBuffers = ptr;
				fixed (global::Interop.SspiCli.SecBuffer* ptr2 = span)
				{
					void* pBuffers2 = ptr2;
					fixed (byte* ptr3 = inSecBuffers._item0.Token)
					{
						void* ptr4 = ptr3;
						fixed (byte* ptr5 = inSecBuffers._item1.Token)
						{
							void* ptr6 = ptr5;
							fixed (byte* ptr7 = inSecBuffers._item2.Token)
							{
								void* ptr8 = ptr7;
								secBufferDesc.pBuffers = pBuffers;
								if (inSecBuffers.Count > 2)
								{
									span2[2].BufferType = inSecBuffers._item2.Type;
									if (inSecBuffers._item2.UnmanagedToken != null)
									{
										span2[2].pvBuffer = inSecBuffers._item2.UnmanagedToken.DangerousGetHandle();
										span2[2].cbBuffer = ((ChannelBinding)inSecBuffers._item2.UnmanagedToken).Size;
									}
									else
									{
										span2[2].cbBuffer = inSecBuffers._item2.Token.Length;
										span2[2].pvBuffer = (IntPtr)ptr8;
									}
								}
								if (inSecBuffers.Count > 1)
								{
									span2[1].BufferType = inSecBuffers._item1.Type;
									if (inSecBuffers._item1.UnmanagedToken != null)
									{
										span2[1].pvBuffer = inSecBuffers._item1.UnmanagedToken.DangerousGetHandle();
										span2[1].cbBuffer = ((ChannelBinding)inSecBuffers._item1.UnmanagedToken).Size;
									}
									else
									{
										span2[1].cbBuffer = inSecBuffers._item1.Token.Length;
										span2[1].pvBuffer = (IntPtr)ptr6;
									}
								}
								if (inSecBuffers.Count > 0)
								{
									span2[0].BufferType = inSecBuffers._item0.Type;
									if (inSecBuffers._item0.UnmanagedToken != null)
									{
										span2[0].pvBuffer = inSecBuffers._item0.UnmanagedToken.DangerousGetHandle();
										span2[0].cbBuffer = ((ChannelBinding)inSecBuffers._item0.UnmanagedToken).Size;
									}
									else
									{
										span2[0].cbBuffer = inSecBuffers._item0.Token.Length;
										span2[0].pvBuffer = (IntPtr)ptr4;
									}
								}
								fixed (byte* ptr9 = outSecBuffer.token)
								{
									outputBuffer.pBuffers = pBuffers2;
									span[0].cbBuffer = outSecBuffer.size;
									span[0].BufferType = outSecBuffer.type;
									span[0].pvBuffer = ((outSecBuffer.token == null || outSecBuffer.token.Length == 0) ? IntPtr.Zero : ((IntPtr)(ptr9 + outSecBuffer.offset)));
									span[1].cbBuffer = 0;
									span[1].BufferType = SecurityBufferType.SECBUFFER_ALERT;
									if ((refContext == null || refContext.IsInvalid) && flag2)
									{
										refContext = new SafeDeleteSslContext();
									}
									result = MustRunAcceptSecurityContext_SECURITY(ref inCredentials, flag2, &secBufferDesc, inFlags, endianness, refContext, ref outputBuffer, ref outFlags, null);
									int index = ((span[0].cbBuffer == 0 && span[1].cbBuffer > 0) ? 1 : 0);
									outSecBuffer.size = span[index].cbBuffer;
									outSecBuffer.type = span[index].BufferType;
									outSecBuffer.token = ((outSecBuffer.size > 0) ? new Span<byte>((void*)span[index].pvBuffer, span[0].cbBuffer).ToArray() : null);
									if (inSecBuffers.Count > 1 && span2[1].BufferType == SecurityBufferType.SECBUFFER_EXTRA && inSecBuffers._item1.Type == SecurityBufferType.SECBUFFER_EMPTY)
									{
										int cbBuffer = span2[1].cbBuffer;
										int num = inSecBuffers._item0.Token.Length - span2[1].cbBuffer;
										span2[0].cbBuffer = cbBuffer;
										span2[0].pvBuffer = span2[0].pvBuffer + num;
										span2[1].BufferType = SecurityBufferType.SECBUFFER_EMPTY;
										span2[1].cbBuffer = 0;
										span[0].cbBuffer = 0;
										if (flag && span[0].pvBuffer != IntPtr.Zero)
										{
											global::Interop.SspiCli.FreeContextBuffer(span[0].pvBuffer);
											span[0].pvBuffer = IntPtr.Zero;
										}
										result = MustRunAcceptSecurityContext_SECURITY(ref inCredentials, flag2, &secBufferDesc, inFlags, endianness, refContext, ref outputBuffer, ref outFlags, null);
										index = ((span[0].cbBuffer == 0 && span[1].cbBuffer > 0) ? 1 : 0);
										if (span[index].cbBuffer > 0)
										{
											if (outSecBuffer.size == 0)
											{
												outSecBuffer.size = span[index].cbBuffer;
												outSecBuffer.type = span[index].BufferType;
												outSecBuffer.token = new Span<byte>((void*)span[index].pvBuffer, span[index].cbBuffer).ToArray();
											}
											else
											{
												byte[] array = new byte[outSecBuffer.size + span[index].cbBuffer];
												Buffer.BlockCopy(outSecBuffer.token, 0, array, 0, outSecBuffer.size);
												new Span<byte>((void*)span[index].pvBuffer, span[index].cbBuffer).CopyTo(new Span<byte>(array, outSecBuffer.size, span[index].cbBuffer));
												outSecBuffer.size = array.Length;
												outSecBuffer.token = array;
											}
										}
										if (span2[1].BufferType == SecurityBufferType.SECBUFFER_EXTRA)
										{
											result = -2146893032;
										}
									}
								}
							}
						}
					}
				}
			}
		}
		finally
		{
			if (flag && span[0].pvBuffer != IntPtr.Zero)
			{
				global::Interop.SspiCli.FreeContextBuffer(span[0].pvBuffer);
			}
			if (span[1].pvBuffer != IntPtr.Zero)
			{
				global::Interop.SspiCli.FreeContextBuffer(span[1].pvBuffer);
			}
		}
		return result;
	}

	private unsafe static int MustRunAcceptSecurityContext_SECURITY(ref SafeFreeCredentials inCredentials, bool isContextAbsent, global::Interop.SspiCli.SecBufferDesc* inputBuffer, global::Interop.SspiCli.ContextFlags inFlags, global::Interop.SspiCli.Endianness endianness, SafeDeleteContext outContext, ref global::Interop.SspiCli.SecBufferDesc outputBuffer, ref global::Interop.SspiCli.ContextFlags outFlags, SafeFreeContextBuffer handleTemplate)
	{
		int num = -2146893055;
		try
		{
			bool success = false;
			inCredentials.DangerousAddRef(ref success);
			outContext.DangerousAddRef(ref success);
			global::Interop.SspiCli.CredHandle credentialHandle = inCredentials._handle;
			global::Interop.SspiCli.CredHandle credHandle = outContext._handle;
			void* ptr = (credHandle.IsZero ? null : (&credHandle));
			isContextAbsent = ptr == null;
			num = global::Interop.SspiCli.AcceptSecurityContext(ref credentialHandle, ptr, inputBuffer, inFlags, endianness, ref outContext._handle, ref outputBuffer, ref outFlags, out var _);
		}
		finally
		{
			if (outContext._EffectiveCredential != inCredentials && (num & 0x80000000u) == 0L)
			{
				outContext._EffectiveCredential?.DangerousRelease();
				outContext._EffectiveCredential = inCredentials;
			}
			else
			{
				inCredentials.DangerousRelease();
			}
			outContext.DangerousRelease();
		}
		if (handleTemplate != null)
		{
			handleTemplate.Set(((global::Interop.SspiCli.SecBuffer*)outputBuffer.pBuffers)->pvBuffer);
			if (handleTemplate.IsInvalid)
			{
				handleTemplate.SetHandleAsInvalid();
			}
		}
		if (isContextAbsent && (num & 0x80000000u) != 0L)
		{
			outContext._handle.SetToInvalid();
		}
		return num;
	}

	internal unsafe static int CompleteAuthToken(ref SafeDeleteSslContext refContext, in SecurityBuffer inSecBuffer)
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(null, $"refContext = {refContext}, inSecBuffer = {inSecBuffer}", "CompleteAuthToken");
		}
		global::Interop.SspiCli.SecBufferDesc inputBuffers = new global::Interop.SspiCli.SecBufferDesc(1);
		int result = -2146893055;
		global::Interop.SspiCli.SecBuffer secBuffer = default(global::Interop.SspiCli.SecBuffer);
		inputBuffers.pBuffers = &secBuffer;
		fixed (byte* ptr = inSecBuffer.token)
		{
			secBuffer.cbBuffer = inSecBuffer.size;
			secBuffer.BufferType = inSecBuffer.type;
			secBuffer.pvBuffer = ((inSecBuffer.unmanagedToken != null) ? inSecBuffer.unmanagedToken.DangerousGetHandle() : ((inSecBuffer.token == null || inSecBuffer.token.Length == 0) ? IntPtr.Zero : ((IntPtr)(ptr + inSecBuffer.offset))));
			global::Interop.SspiCli.CredHandle credHandle = ((refContext != null) ? refContext._handle : default(global::Interop.SspiCli.CredHandle));
			if ((refContext == null || refContext.IsInvalid) && credHandle.IsZero)
			{
				refContext = new SafeDeleteSslContext();
			}
			bool success = false;
			try
			{
				refContext.DangerousAddRef(ref success);
				result = global::Interop.SspiCli.CompleteAuthToken(credHandle.IsZero ? null : (&credHandle), ref inputBuffers);
			}
			finally
			{
				if (success)
				{
					refContext.DangerousRelease();
				}
			}
		}
		return result;
	}

	internal unsafe static int ApplyControlToken(ref SafeDeleteContext refContext, in SecurityBuffer inSecBuffer)
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(null, $"refContext = {refContext}, inSecBuffer = {inSecBuffer}", "ApplyControlToken");
		}
		int result = -2146893055;
		fixed (byte* ptr = inSecBuffer.token)
		{
			global::Interop.SspiCli.SecBufferDesc inputBuffers = new global::Interop.SspiCli.SecBufferDesc(1);
			global::Interop.SspiCli.SecBuffer secBuffer = default(global::Interop.SspiCli.SecBuffer);
			inputBuffers.pBuffers = &secBuffer;
			secBuffer.cbBuffer = inSecBuffer.size;
			secBuffer.BufferType = inSecBuffer.type;
			secBuffer.pvBuffer = ((inSecBuffer.unmanagedToken != null) ? inSecBuffer.unmanagedToken.DangerousGetHandle() : ((inSecBuffer.token == null || inSecBuffer.token.Length == 0) ? IntPtr.Zero : ((IntPtr)(ptr + inSecBuffer.offset))));
			global::Interop.SspiCli.CredHandle credHandle = ((refContext != null) ? refContext._handle : default(global::Interop.SspiCli.CredHandle));
			if ((refContext == null || refContext.IsInvalid) && credHandle.IsZero)
			{
				refContext = new SafeDeleteSslContext();
			}
			bool success = false;
			try
			{
				refContext.DangerousAddRef(ref success);
				result = global::Interop.SspiCli.ApplyControlToken(credHandle.IsZero ? null : (&credHandle), ref inputBuffers);
			}
			finally
			{
				if (success)
				{
					refContext.DangerousRelease();
				}
			}
		}
		return result;
	}
}
