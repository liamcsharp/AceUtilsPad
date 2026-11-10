using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

namespace AUP
{
	public sealed class DiscordRPC : MonoBehaviour
	{
		private const string ApplicationId = "1502771963385483494";
		private const string LargeImageKey = "";
		private const string LargeImageText = "AceUtilsPad";

		private const int ReconnectDelayMs = 10000;
		private const int MaxPipeIndex = 10;

		private Thread ioThread;
		private volatile bool running;
		private volatile bool connected;

		private readonly object activityLock = new object();
		private string pendingDetails = "Using Menu";
		private string pendingState = "In Game";
		private bool activityDirty = true;

		private long startTimestamp;

		private NamedPipeClientStream pipe;

		private void Awake()
		{
			this.startTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
			this.running = true;
			this.ioThread = new Thread(this.IoLoop)
			{
				IsBackground = true,
				Name = "AUP-DiscordRPC"
			};
			this.ioThread.Start();
		}

		public void SetActivity(string details, string state)
		{
			lock (this.activityLock)
			{
				if (details == this.pendingDetails && state == this.pendingState)
				{
					return;
				}

				this.pendingDetails = details;
				this.pendingState = state;
				this.activityDirty = true;
			}
		}

		private void IoLoop()
		{
			while (this.running)
			{
				try
				{
					if (!this.connected)
					{
						if (!this.TryConnect())
						{
							Thread.Sleep(ReconnectDelayMs);
							continue;
						}

						lock (this.activityLock)
						{
							this.activityDirty = true;
						}
					}

					bool needSend;
					lock (this.activityLock)
					{
						needSend = this.activityDirty;
					}

					if (needSend)
					{
						this.SendActivity();
						lock (this.activityLock)
						{
							this.activityDirty = false;
						}
					}

					Thread.Sleep(500);
				}
				catch (Exception ex)
				{
					UnityEngine.Debug.LogWarning("[AceUtilsPad] Discord RPC connection lost: " + ex.Message);
					this.Cleanup();
					if (this.running)
					{
						Thread.Sleep(ReconnectDelayMs);
					}
				}
			}
		}

		private bool TryConnect()
		{
			for (int i = 0; i < MaxPipeIndex; i++)
			{
				NamedPipeClientStream candidate = null;
				try
				{
					candidate = new NamedPipeClientStream(".", "discord-ipc-" + i, PipeDirection.InOut,
						PipeOptions.Asynchronous);
					candidate.Connect(200);

					this.pipe = candidate;
					this.Handshake();
					this.connected = true;
					UnityEngine.Debug.Log("[AceUtilsPad] Discord RPC connected on discord-ipc-" + i);
					return true;
				}
				catch
				{
					if (candidate != null)
					{
						try
						{
							candidate.Dispose();
						}
						catch
						{
						}
					}
				}
			}

			return false;
		}

		private void Handshake()
		{
			JObject handshake = new JObject
			{
				["v"] = 1,
				["client_id"] = ApplicationId
			};
			this.WriteFrame(0, handshake.ToString(Formatting.None));
			this.ReadFrame();
		}

		private void SendActivity()
		{
			string details;
			string state;
			lock (this.activityLock)
			{
				details = this.pendingDetails;
				state = this.pendingState;
			}

			JObject activity = new JObject
			{
				["details"] = details,
				["state"] = state,
				["timestamps"] = new JObject
				{
					["start"] = this.startTimestamp
				}
			};

			if (!string.IsNullOrEmpty(LargeImageKey))
			{
				activity["assets"] = new JObject
				{
					["large_image"] = LargeImageKey,
					["large_text"] = LargeImageText
				};
			}

			JObject payload = new JObject
			{
				["cmd"] = "SET_ACTIVITY",
				["args"] = new JObject
				{
					["pid"] = Process.GetCurrentProcess().Id,
					["activity"] = activity
				},
				["nonce"] = Guid.NewGuid().ToString()
			};

			this.WriteFrame(1, payload.ToString(Formatting.None));
			this.ReadFrame();
		}

		private void WriteFrame(int opcode, string json)
		{
			if (this.pipe == null || !this.pipe.IsConnected)
			{
				throw new IOException("Discord pipe is not connected.");
			}

			byte[] body = Encoding.UTF8.GetBytes(json);
			byte[] frame = new byte[8 + body.Length];
			Buffer.BlockCopy(BitConverter.GetBytes(opcode), 0, frame, 0, 4);
			Buffer.BlockCopy(BitConverter.GetBytes(body.Length), 0, frame, 4, 4);
			Buffer.BlockCopy(body, 0, frame, 8, body.Length);
			this.pipe.Write(frame, 0, frame.Length);
			this.pipe.Flush();
		}

		private void ReadFrame()
		{
			byte[] header = this.ReadExactly(8);
			if (header == null)
			{
				return;
			}

			int length = BitConverter.ToInt32(header, 4);
			if (length > 0)
			{
				this.ReadExactly(length);
			}
		}

		private byte[] ReadExactly(int count)
		{
			byte[] buffer = new byte[count];
			int offset = 0;
			while (offset < count)
			{
				int read = this.pipe.Read(buffer, offset, count - offset);
				if (read <= 0)
				{
					throw new IOException("Discord pipe closed while reading.");
				}

				offset += read;
			}

			return buffer;
		}

		private void Cleanup()
		{
			this.connected = false;
			if (this.pipe != null)
			{
				try
				{
					this.pipe.Dispose();
				}
				catch
				{
				}

				this.pipe = null;
			}
		}

		private void OnDestroy()
		{
			this.Shutdown();
		}

		private void OnApplicationQuit()
		{
			this.Shutdown();
		}

		private void Shutdown()
		{
			if (!this.running)
			{
				return;
			}

			this.running = false;
			try
			{
				if (this.pipe != null && this.pipe.IsConnected)
				{
					this.WriteFrame(2, "{}");
				}
			}
			catch
			{
			}

			this.Cleanup();
		}
	}
}
