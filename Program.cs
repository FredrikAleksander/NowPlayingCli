using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using Windows.Media;
using Windows.Media.Control;

namespace NowPlayingCli
{
    class Program
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetConsoleOutputCP(uint wCodePageID);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetConsoleCP(uint wCodePageID);

        static string PrintMediaProperties(GlobalSystemMediaTransportControlsSessionMediaProperties props, string videoSymbol, string musicSymbol)
        {
            if (props.PlaybackType.HasValue)
            {
                switch (props.PlaybackType.Value)
                {
                    case MediaPlaybackType.Music:
                        return $"{musicSymbol} {props.Title} by {props.Artist}";
                    case MediaPlaybackType.Video:
                        return $"{videoSymbol} {props.Title}";

                }
            }
            return "";
        }

        static readonly string[] defaultPrograms = { "spotify.exe" };
        static readonly MediaPlaybackType[] defaultPlaybackTypes = { MediaPlaybackType.Music, MediaPlaybackType.Video };

        static void PrintUsage(string program, TextWriter writer)
        {
            writer.WriteLine($"{program} [OPTIONS] [PROGRAMS...]");
            writer.WriteLine($"  Where OPTIONS may be any of the following");
            writer.WriteLine($"    -h,--help                 This text");
            writer.WriteLine($"    -t,--type <video|music>   Add playback type to list of types that is desired");
            writer.WriteLine($"    -m,--icon-music <ICON>    Use ICON as the icon for music playback");
            writer.WriteLine($"    -v,--icon-video <ICON>    Use ICON as the icon for video playback");
            writer.WriteLine($"    -d,--listen     <PORT>    Instead of printing to the console, listen \n" +
                             $"                              for TCP connections on <PORT>, printing media\n" +
                             $"                              information, then closing the connection\n");
            writer.WriteLine($"  And PROGRAMS is a list of executables to listen to media events from (defaults to spotify.exe)");
        }

        static int Listen(int port, Func<string> callback)
        {
            var listener = TcpListener.Create(port);

            try
            {
                listener.Start();

                while (true)
                {
                    var handler = listener.AcceptSocket();
                    byte[] msg = Encoding.UTF8.GetBytes(callback());

                    handler.Send(msg);
                    handler.Shutdown(SocketShutdown.Both);
                    handler.Close();
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            return 0;
        }

        static int Main(string[] args)
        {
            SetConsoleOutputCP(65001);
            SetConsoleCP(65001);

            var programName = $"{Path.GetFileNameWithoutExtension(Environment.GetCommandLineArgs()[0])}.exe";

            var videoSymbol = "🎬";
            var musicSymbol = "🎵";

            int? listenPort = null;

            var userPrograms = new List<string>();
            var userPlaybackTypes = new List<MediaPlaybackType>();

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].StartsWith("-"))
                {
                    if (args[i] == "-h" || args[i] == "--help")
                    {
                        PrintUsage(programName, Console.Out);
                        return 0;
                    }
                    else if (args[i] == "-d" || args[i] == "--listen")
                    {
                        if (args.Length > i + 1)
                        {
                            if(int.TryParse(args[i+1], out var result))
                            {
                                listenPort = result;
                            }
                            else
                            {
                                Console.Error.WriteLine($"Not a valid port: {args[i+1]}");
                                PrintUsage(programName, Console.Error);
                                return 1;
                            }
                            i++;
                        }
                        else
                        {
                            Console.Error.WriteLine($"Incomplete parameter: {args[i]}");
                            PrintUsage(programName, Console.Error);
                            return 1;
                        }
                    }
                    else if (args[i] == "-m" || args[i] == "--icon-music")
                    {
                        if(args.Length > i + 1)
                        {
                            musicSymbol = args[i + 1];
                            i++;
                        }
                        else
                        {
                            Console.Error.WriteLine($"Incomplete parameter: {args[i]}");
                            PrintUsage(programName, Console.Error);
                            return 1;
                        }
                    }
                    else if (args[i] == "-v" || args[i] == "--icon-video")
                    {
                        if (args.Length > i + 1)
                        {
                            videoSymbol = args[i + 1];
                            i++;
                        }
                        else
                        {
                            Console.Error.WriteLine($"Incomplete parameter: {args[i]}");
                            PrintUsage(programName, Console.Error);
                            return 1;
                        }
                    }
                    else if (args[i] == "-t" || args[i] == "--type")
                    {
                        if (args.Length > i + 1)
                        {
                            if (args[i + 1] == "video")
                            {
                                userPlaybackTypes.Add(MediaPlaybackType.Video);
                            }
                            else if (args[i + 1] == "music")
                            {
                                userPlaybackTypes.Add(MediaPlaybackType.Music);
                            }
                            else
                            {
                                Console.Error.WriteLine($"Unknown playback type: {args[i + 1]}");
                                PrintUsage(programName, Console.Error);
                            }
                            i++;
                        }
                        else
                        {
                            Console.Error.WriteLine($"Incomplete parameter: {args[i]}");
                            PrintUsage(programName, Console.Error);
                            return 1;
                        }
                    }
                    else
                    {
                        Console.Error.WriteLine($"Unknown parameter: {args[i]}");
                        PrintUsage(programName, Console.Error);
                        return 1;
                    }
                }
                else
                {
                    userPrograms.Add(args[i]);
                }
            }


            IEnumerable<string> programs = userPrograms.Count > 0 ? (IEnumerable<string>)userPrograms : defaultPrograms;
            IEnumerable<MediaPlaybackType> playbackTypes = userPlaybackTypes.Count > 0 ? (IEnumerable<MediaPlaybackType>)userPlaybackTypes : defaultPlaybackTypes;

            var sessionManager = GlobalSystemMediaTransportControlsSessionManager.RequestAsync().GetAwaiter().GetResult();
            Func<string> printer = () =>
            {
                var currentSession = sessionManager.GetCurrentSession();
                if (programs.Contains(currentSession?.SourceAppUserModelId.ToLower()))
                {
                    var mediaProperties = currentSession.TryGetMediaPropertiesAsync().GetAwaiter().GetResult();
                    if (mediaProperties?.PlaybackType.HasValue == true && playbackTypes.Contains(mediaProperties.PlaybackType.Value))
                    {
                        return PrintMediaProperties(mediaProperties, videoSymbol, musicSymbol);
                    }
                }
                return "";
            };

            if (listenPort.HasValue)
            {
                Console.WriteLine($"Listening for requests on port: {listenPort.Value}");
                Listen(listenPort.Value, printer);
            }
            else
            {
                Console.WriteLine(printer());
            }

            return 0;
        }
    }
}
