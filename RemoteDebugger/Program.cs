﻿using System;
using System.Net.Sockets;
using System.Text;
using System.Timers;
using CommandLine;

namespace RemoteDebugger
{
	class Program
	{
		public class ArgumentBuilder
		{
			[Option('i', "infinite", Default = false, HelpText = "If set to true, it will always try to reconnect.", Required = false)]
			public bool argInfinite { get; set; }

			[Option('a', "attempts", Default = 1, HelpText = "How many times it should try to reconnect.", Required = false)]
			public int argAttempts { get; set; }


			[Option("ip", Default = "192.168.49.1", HelpText = "The IP of the Robot Controller.", Required = false)]
			public string argIp { get; set; }


			[Option('p', "port", Default = 8333, HelpText = "The port that is listening on the Robot Controller.", Required = false)]
			public int argPort { get; set; }
		}

		static NetworkStream stream;
		static bool heartbeatFailed = false;
		static System.Timers.Timer aTimer;

		static void Main(string[] args)
		{
			aTimer = new System.Timers.Timer(10000);
			aTimer.Enabled = false;
			aTimer.Elapsed += SendHeartbeet;
			aTimer.AutoReset = true;
			Console.WriteLine("Starting connection...");
			Parser.Default.ParseArguments<ArgumentBuilder>(args)
				   .WithParsed(options =>
				   {
					   if (options.argInfinite)
					   {
						   ConnectForever(options.argIp, options.argPort);
					   }
					   else
					   {
						   Connect(options.argIp, options.argPort, options.argAttempts);
					   }
					   Console.WriteLine("Either a fatal error has occured or there have been too many attempts, restarting");
				   });
		}
		static void ConnectForever(string server, int port)
		{
			while (true)
			{
				Connect(server, port, 1);
				System.Threading.Thread.Sleep(500);
			}
		}
		static void Connect(string server, int port, int attempts = 1)
		{
			for (int i = 0; i < attempts; i++)
			{


				try
				{
					// Create a TcpClient.
					// Note, for this client to work you need to have a TcpServer 
					// connected to the same address as specified by the server, port
					// combination.

					TcpClient client = new TcpClient(server, port);

					// Get Stream
					stream = client.GetStream();

					aTimer.Enabled = true;

					// Buffer to store the response bytes.
					byte[] data = new byte[256];

					// String to store the response ASCII representation.
					String responseString = String.Empty;

					// Read the first batch of the TcpServer response bytes.
					Console.WriteLine("Connection made!");
					while (true)
					{
						try
						{
							if (stream.DataAvailable)
							{
								Int32 byteResponse = stream.Read(data, 0, data.Length);
								responseString = Encoding.ASCII.GetString(data, 0, byteResponse);
								if (!String.IsNullOrWhiteSpace(responseString))
								{
									Console.Write(responseString);
								}
							}
							if(!client.Connected || heartbeatFailed)
							{
								break;
							}
						}
						catch (Exception e)
						{
							Console.WriteLine("[!] Error caught, trivial or undetermined error level!");
							Console.WriteLine(e.Message);
							Console.WriteLine(e.StackTrace);
						}
						System.Threading.Thread.Sleep(100);
					}

					Console.WriteLine("[!!!] Connection Lost!");
					// Close everything.
					aTimer.Enabled = false;
					stream.Close();
					stream = null;
					client.Close();
				}
				catch (ArgumentNullException e)
				{
					Console.WriteLine("ArgumentNullException: {0}", e);
				}
				catch (SocketException e)
				{
					//if"No connection could be made because the target machine actively refused it"")
					Console.WriteLine("SocketException: {0}", e.Message);
				}
			}
		}

		private static void SendHeartbeet(object sender, ElapsedEventArgs e)
		{
			if(!stream.CanWrite || !stream.CanRead || stream == null)
			{
				heartbeatFailed = true;
				return;
			}
			{
				try
				{
					byte[] ping = Encoding.ASCII.GetBytes("ping");
					stream.Write(ping, 0, ping.Length);
					stream.Flush();
				}
				catch (SocketException er)
				{
					Console.WriteLine("[!] Socket Error caught during heartbeet!");
					Console.WriteLine(er.Message);
					Console.WriteLine(er.StackTrace);
					heartbeatFailed = true;
				}
				catch (Exception er)
				{
					Console.WriteLine("[!] General Error caught during heartbeet!");
					Console.WriteLine(er.Message);
					Console.WriteLine(er.StackTrace);
				}
			}
		}
	}
}