using System;
using System.IO;
using System.Data.SQLite;
using System.Net.Http;      // Http

// Media reproduction
using SharpDX.Multimedia;
using SharpDX.XAudio2;

// Text to speech
using System.Speech.Synthesis;

// C# Library for Twitch's API interaction
using TwitchLib.Client;
using TwitchLib.Client.Enums;
using TwitchLib.Client.Events;
using TwitchLib.Client.Extensions;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Models;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;

namespace TwitchBot
{

    class RecursosComunes
    {
        List<String> mods = null;
        List<String> vips = null;

        public List<String> GetChannelModerators()
        {
            return mods;
        }

        public List<String> GetChannelVips()
        {
            return vips;
        }



    }

    class Fraserio
    {
        public const int Amor = 1;
        static private string[] AmorCollection = { "un cariño residual", "un picor en el aparato de ", "un amor indiscutible de", "un amor insano de", "una obsesión romántica de" };
        static private string[] OdioCollection = { "un pollion en tu culo", "te quiero tanto que te deseo libre, volando todo lo alto que puedas", "he tenido almorranas más majas" }; //trabajo en equipo
        static private string[] FunnyVoiceCollection = { "", "" };
        //methods
        static public string getFrase(int coleccion)
        {
            switch (coleccion)
            {
                case 0:
                    return OdioCollection[(new Random().Next() % OdioCollection.Length)];
                case 1:
                    return AmorCollection[(new Random().Next() % AmorCollection.Length)];
                default:
                    return "Error: Bad Use";
            }
        }
    }

    class Command_Args_2
    {
        public string argData;
        public OnMessageReceivedArgs e = null;
        public HttpClient httpClient = null;
        int index;  // For a "strtok" like behaviour

        public Command_Args_2(string argumentRaw, int initialindex = 0)
        {
            argData = argumentRaw;
            index = initialindex;
        }

        public void Reset()
        {
            index = 0;
        }

        public string NextArg()
        {
            int firstPos = index;
            
            while (index < argData.Length && argData[index] != ' ')
                index++;

            return argData.Substring(firstPos, index++ - firstPos);
        }

        public string[] ArgList()
        {
            return argData.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        }

        internal string RemainingArgs()
        {
            return argData.Substring(index);
        }
    }

    class Command_Args
    {
        public string[] argList;
        public OnMessageReceivedArgs e = null;
        public HttpClient httpClient = null;
        int initial_arg;

        public Command_Args(string[] args, int first_index = 1, OnMessageReceivedArgs e_args = null, HttpClient h_client = null)
        {
            argList = args;
            initial_arg = first_index;
            e = e_args;
            httpClient = h_client;
        }

        public int ArgCount()
        {
            return argList.Length - initial_arg;
        }

        public string GetArg()
        {
            return argList[initial_arg];
        }

    }

    abstract class Command
    {
        protected string command_name;
        protected int cooldown;
        protected DateTime cd_start = DateTime.UnixEpoch;
        protected bool blocked = false;
        protected bool inCooldown = false;
        protected bool enabled = true;

        virtual public bool Load()
        {
            if (enabled)
                Utilidades.ConsoleHappyWrite(new Tuple<string, ConsoleColor>[] {Tuple.Create("Command" + " " + command_name + ":" + " ", ConsoleColor.White), Tuple.Create("OK", ConsoleColor.Green)});
            else
                Utilidades.ConsoleHappyWrite(new Tuple<string, ConsoleColor>[] { Tuple.Create("Command" + " " + command_name + ":" + " ", ConsoleColor.White), Tuple.Create("DISABLED", ConsoleColor.White) });
            return enabled;
        }

        virtual public bool IsCommand(string command) => (command == command_name ? true : false);

        public void Error(TwitchClient tclient, string channel, int type)
        {
            tclient.SendMessage(channel,
                type switch
                {
                    0 => "",
                    1 => "",
                    2 => "",
                    _ => ""
                }
            );
        }

        public void ResetCD() => cd_start = DateTime.UnixEpoch;

        virtual public bool IsCDOver()
        {
            if (DateTime.UtcNow.CompareTo(cd_start.AddSeconds(cooldown)) <= 0 || cd_start == DateTime.UnixEpoch)
            {
                ResetCD();
                return true;
            }
            else
                return false;
        }

        public abstract void Action(TwitchClient tclient, string channel, Command_Args args);

    };

    class Hola : Command
    {
        public Hola(int coldown)
        {
            this.command_name = "hola";
            this.cooldown = coldown;
        }

        override public void Action(TwitchClient tclient, string channel, Command_Args args)
        {
            if (IsCDOver())
            {
                tclient.SendMessage(channel, "Hola a ti también @" + args.e.ChatMessage.DisplayName + ", bienvenido ^^");
                this.cd_start = DateTime.Now;
            }
        }
    }

    class Azar : Command
    {
        public Azar(int coldown)
        {
            this.command_name = "azar";
            this.cooldown = coldown;
        }

        override async public void Action(TwitchClient tclient, string channel, Command_Args args)
        {
            // TODO: Tengo 1000 peticiones a random.org diarias y tengo 250.000 bits (datos individuales) por dia, deberia cuando vea que esto se usa
            //      lo suficiente, utilizar peticiones pidiendo muchos bits de una y guardarlos para posteriores consultas
            string choosenOne = "";
            int data_start = 0;
            int i = 0;
            string[] possibilities;

            if (args.argList.Length != 1)  // quiere escoger los resultados posibles
            {
                List<string> aux1 = new List<string>();
                for (i = 1; i < args.argList.Length; i++)
                    aux1.Add(args.argList[i]);
                possibilities = aux1.ToArray();
            }
            else                  // tirada de moneda tipica
                possibilities = new string[] { "Cara", "Cruz"};

            try
            {
                HttpContent request_headers = new StringContent("{" +
                    "\"jsonrpc\": \"2.0\"," +
                    "\"method\": \"generateIntegers\"," +
                    "\"params\": {" +
                                    "\"apiKey\": \"" + Resource1.random_org_key+"\"," +
                        "\"n\": 1," +
                        "\"min\": 0," +
                        "\"max\": " + (possibilities.Length - 1) + "," +
                        "\"replacement\": true" +
                    "}," +
                    "\"id\": 42" +
                "}");
                request_headers.Headers.ContentType.MediaType = "application/json";
                HttpResponseMessage response = await args.httpClient.PostAsync("https://api.random.org/json-rpc/4/invoke", request_headers);
                Console.WriteLine(await response.Content.ReadAsStringAsync());
                string response_content = await response.Content.ReadAsStringAsync();
                data_start = (response_content.IndexOf("\"data\":[") + "\"data\":[".Length);
                i = 0;
                while (true)
                {
                    if (response_content[data_start + i] == ']')
                        break;
                    choosenOne += response_content[data_start + i];
                    i++;
                }

                Console.WriteLine(choosenOne);
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();
                // Above three lines can be replaced with new helper method below
                // string responseBody = await client.GetStringAsync(uri);

                //Console.WriteLine(possibilities[choosenOne]);
                tclient.SendMessage(channel, possibilities[int.Parse(choosenOne)]);
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine("\nException Caught!");
                Console.WriteLine("Message :{0} ", e.Message);
            }
        }
    }

    class Voice : Command
    {

        public Voice(int coldown = 1 * 60 * 1000)
        {
            this.command_name = "voz";
            cooldown = coldown;
        }

        public override void Action(TwitchClient tclient, string channel, Command_Args args)
        {
            SpeechSynthesizer synth = new SpeechSynthesizer();

            // Configure the audio output.   
            synth.SetOutputToDefaultAudioDevice();

            // Speak a string.  
            if(args.ArgCount() > 0)
                synth.Speak(args.GetArg());
            else
                synth.Speak("Yo no anderstán, prueba a decirme lo que quieres que diga :)");
        }
    }

    class Sounds : Command
    {

        public static string path = "M:\\Javi\\Escritorio\\Streaming\\Sonidos\\sonidos\\";

        List<Tuple<string, int, bool>> soundCollection;

        ConcurrentQueue<int> soundQueue;

        // (filename, cooldown, isInCooldown)
        public static List<Tuple<string, int, bool>> SoundCollector()
        {
            List<Tuple<string, int, bool>> aux = new List<Tuple<string, int, bool>>();

            foreach (var item in Directory.EnumerateFiles(Sounds.path))
            {
                aux.Add(new (item.Substring(Sounds.path.Length, item.Length - Sounds.path.Length - 4), 60*1000, false));
            }

            return aux;
        }

        public static List<Tuple<string, int, bool>> SoundCollector(string path)
        {
            List<Tuple<string, int, bool>> aux = new List<Tuple<string, int, bool>>();

            foreach (var item in Directory.EnumerateFiles(path))
            {
                aux.Add(new (item.Substring(path.Length, item.Length - path.Length - 4), 60*1000, false));
            }

            return aux;
        }
        
        public Sounds(ConcurrentQueue<int> queue, List<Tuple<string, int, bool>> lista = null, int coldown = 1 * 60 * 1000)
        {
            this.soundQueue = queue;
            this.soundCollection = lista;
            this.command_name = "p";
            this.cooldown = coldown;
        }

        /*
        override public bool IsCommand(string command)
        {
            string lower_command = command.ToLower();
            for (int i = 0; i < sounds.Length; i++)
            {
                if (lower_command == sounds[i])
                {
                    option = i; return true;
                }
            }

            return false;
        }
        */

        public static void PLaySoundFile(XAudio2 device, string text, string fileName)
        {
            Console.WriteLine("{0} => {1} (Press esc to skip)", text, fileName);
            var stream = new SoundStream(File.OpenRead(fileName));
            var waveFormat = stream.Format;
            var buffer = new AudioBuffer
            {
                Stream = stream.ToDataStream(),
                AudioBytes = (int)stream.Length,
                Flags = BufferFlags.EndOfStream
            };
            stream.Close();

            var sourceVoice = new SourceVoice(device, waveFormat, true);
            // Adds a sample callback to check that they are working on source voices
            sourceVoice.BufferEnd += (context) => Console.WriteLine(" => event received: end of buffer");
            sourceVoice.SubmitSourceBuffer(buffer, stream.DecodedPacketsInfo);
            sourceVoice.Start();

            int count = 0;
            while (sourceVoice.State.BuffersQueued > 0)
            {
                if (count == 50)
                {
                    Console.Write(".");
                    Console.Out.Flush();
                    count = 0;
                }
                Thread.Sleep(10);
                count++;
            }
            Console.WriteLine();

            sourceVoice.DestroyVoice();
            sourceVoice.Dispose();
            buffer.Stream.Dispose();
        }

        public override void Action(TwitchClient tclient, string channel, Command_Args args)
        {
            string aux = null;
            int i = 0;

            if (args.ArgCount() == 0)
            {
                foreach (var item in soundCollection)
                {
                    aux += item.Item1+" ";
                }
                tclient.SendMessage(channel, aux);
                return;
            }

            foreach (var item in soundCollection)
            {
                if(item.Item1 == args.GetArg())
                {
                    soundQueue.Enqueue(i);
                    i = 0;
                    continue;
                }
                i++;
            }
        }

        public void Action2(TwitchClient tclient, string channel, Command_Args args)
        {
            string aux = null;
            var xaudio2 = new XAudio2();
            var masteringVoice = new MasteringVoice(xaudio2);

            if (args.ArgCount() == 0)
            {
                foreach (var item in Directory.EnumerateFiles(path))
                {
                    aux+=(item.Substring(path.Length, item.Length-path.Length-4)+" ");
                }
                tclient.SendMessage(channel, aux);
                return;
            }

            try
            {

                PLaySoundFile(xaudio2, "1) Playing a standard WAV file", path+args.GetArg()+".wav");

                masteringVoice.Dispose();
                xaudio2.Dispose();

            }
            catch (Exception e)
            {
                Console.WriteLine("Error while playing sounds" + e);
            }
        }
    }

    class Vote : Command
    {
        public Vote(int coldown = 0)
        {
            this.command_name = "vote";
            this.cooldown = 0;
        }

        public override void Action(TwitchClient tclient, string channel, Command_Args args)
        {
            throw new NotImplementedException();
        }
    }

    class Quotes : Command
    {

        int previousQuote = -1;
        string database = @"URI=file:M:\Javi\Escritorio\Streaming\TwitchBot\TwitchBot\SQL_files\quoteDB.db";

        public Quotes(int coldown)
        {
            this.command_name = "quotes";
            this.cooldown = coldown;
        }

        public void Action2(TwitchClient tclient, string channel, Command_Args_2 args)
        {
            string aux = args.NextArg();    // command name
            Console.WriteLine("quotes");
            if ((aux = args.NextArg())==string.Empty)
            {
                using var con = new SQLiteConnection(database);
                con.Open();

                string stm = "SELECT id, q_date, quote FROM quotes ORDER BY random() LIMIT 1";

                using var cmd = new SQLiteCommand(stm, con);
                using SQLiteDataReader rdr = cmd.ExecuteReader();

                rdr.Read();

                previousQuote = rdr.GetInt32(0);

                tclient.SendMessage(channel, rdr.GetString(1)+rdr.GetString(2));
                Console.WriteLine("quotes");
            }
            else
            {
                if(aux == "add")
                {
                    using var con = new SQLiteConnection(database);
                    con.Open();
                    aux = args.RemainingArgs();
                    if(aux != string.Empty)
                    {
                        string stm = "INSERT INTO quotes (id, context, q_date, quote) VALUES (CURRENT_DATE,\""+""+"\",\""+ aux +"\")";

                        using var cmd = new SQLiteCommand(stm, con);
                        //cmd.Parameters.Add(aux); with "?"
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }

        public override void Action(TwitchClient tclient, string channel, Command_Args args)
        {
            Action2(tclient, channel, new Command_Args_2("quotes add Hola que tal",6));
            return;
            if (args.ArgCount() > 0)        // Quiere posiblemente, insertar una nueva quote
            {
                if (args.argList[1].ToLower() == "add")    // Añadir quote
                {
                    
                }
                throw new NotImplementedException();
            }
            else
            {
                
                using var con = new SQLiteConnection(database);
                con.Open();

                string stm = "SELECT id, q_date, quote FROM quotes ORDER BY random() LIMIT 1";

                using var cmd = new SQLiteCommand(stm, con);
                using SQLiteDataReader rdr = cmd.ExecuteReader();

                rdr.Read();

                previousQuote = rdr.GetInt32(0);

                tclient.SendMessage(channel, rdr.GetString(1));
            }
        }
    }

    class Traducir : Command
    {
        public Traducir(int coldown)
        {
            this.command_name = "traducir";
            this.cooldown = coldown;
        }

        override async public void Action(TwitchClient tclient, string channel, Command_Args args)
        {
            throw new NotImplementedException();
        }
    }

    class Chiste : Command
    {
        int previousJoke = -1;
        string database = @"URI=file:M:\Javi\Escritorio\Streaming\TwitchBot\TwitchBot\SQL_files\jokeDB.db";

        public Chiste(int coldown)
        {
            this.command_name = "chiste";
            this.cooldown = coldown;
        }

        override public void Action(TwitchClient tclient, string channel, Command_Args args)
        {
            if(args.ArgCount() > 0)        // Quiere posiblemente, valorar un chiste
            {
                if(args.argList[1].ToLower() == "add")    // Añadir chiste
                {
                    
                }
                throw new NotImplementedException();
                //tclient.SendMessage(channel, "El chiste se revisará, gracias por el feedback, amable persona"); // Sistema de marcado de chistes malos
            }
            else
            {

                using var con = new SQLiteConnection(database);
                con.Open();

                string stm = "SELECT id,joke FROM chistes ORDER BY random() LIMIT 1";

                using var cmd = new SQLiteCommand(stm, con);
                using SQLiteDataReader rdr = cmd.ExecuteReader();

                rdr.Read();

                previousJoke = rdr.GetInt32(0);

                tclient.SendMessage(channel, rdr.GetString(1));
            }
        }
    }

    class Discord : Command
    {
        public Discord(int coldown)
        {
            this.command_name = "discord";
            this.cooldown = coldown;
        }

        override public void Action(TwitchClient tclient, string channel, Command_Args args)
        {
            tclient.SendMessage(channel, "Únete al server de discord en el siguiente enlace ^^:" + Resource1.discord_link);
        }
    }

    class Promo : Command
    {
        public Promo(int coldown)
        {
            this.command_name = "promo";
            this.cooldown = coldown;
        }

        override public void Action(TwitchClient tclient, string channel, Command_Args args)
        {
            if (args.ArgCount() > 0)
                tclient.SendMessage(channel, "Porfa, seguid a @" + args.GetArg() + " en su canal de Twitch <3 <3");
            else
                throw new NotImplementedException();    //Error
        }
    }

    class Commandos : Command
    {
        public Commandos(int coldown)
        {
            this.command_name = "commandos";
            this.cooldown = coldown;
        }
        override public void Action(TwitchClient tclient, string channel, Command_Args args)
        {
            tclient.SendMessage(channel, "!commandos");
            tclient.SendMessage(channel, "!Hola");
            tclient.SendMessage(channel, "!Promo <nombre_usuario>");
            tclient.SendMessage(channel, "!Grillos");
            tclient.SendMessage(channel, "!Niputagracia");
            tclient.SendMessage(channel, "!Translate <origen> <idioma_destino>");
            tclient.SendMessage(channel, "!Cortana <narración>");
            tclient.SendMessage(channel, "!Discord");
            tclient.SendMessage(channel, "!Chiste");
        }
    }

    class Croqueta : Command        //"PitonisaCroqueta"
    {

        public Croqueta(int coldown)
        {
            this.command_name = "croqueta";
            this.cooldown = coldown;
        }

        public override void Action(TwitchClient tclient, string channel, Command_Args aux)
        {
            if (aux.argList.Length < 3)
                Error(tclient, channel, 0);
            else
            {
                tclient.SendMessage(channel, (aux.argList[1][0] == '@' ? "" : "@")
                    +
                    aux.argList[1] + " siente " + Fraserio.getFrase(Fraserio.Amor) + " " +
                    (aux.argList[2].ToUpper() == "YUKI" ? "200" : ((new Random()).Next() % 100)) + "% por "
                    +
                    (aux.argList[2][0] == '@' ? "" : "@") + aux.argList[2]);
            }
        }
    }

    class Pitonisio : Command
    {
        public Pitonisio(int coldown)
        {
            this.command_name = "pitonisio";
            this.cooldown = coldown;
        }

        public override void Action(TwitchClient tclient, string channel, Command_Args args)
        {
            if (args.argList.Length < 3)
                Error(tclient, channel, 0);
            else
            {
                tclient.SendMessage(channel, (args.argList[1][0] == '@' ? "" : "@")
                    +
                    args.argList[1] + " siente " + Fraserio.getFrase(Fraserio.Amor) + " " + (new Random().Next() % 100) + "% por "
                    +
                    (args.argList[2][0] == '@' ? "" : "@") + args.argList[2]);
            }
        }
    }

    class Program
    {
        internal static bool debug = false;
        static string version = "0.2";
        static string release = "0";
        
        static void ConsoleHappyWrite(Tuple<string, ConsoleColor>[] tuple)
        {
            foreach (var item in tuple)
            {
                Console.ForegroundColor = item.Item2;
                Console.WriteLine(item.Item1);
            }
            Console.ResetColor();
        }

        static void Main(string[] args)
        {
            Bot bot = new Bot();
            ConsoleHappyWrite(new Tuple<string, ConsoleColor>[]{ Tuple.Create(("Twitch Bot initiated:" + "\n" + "Version: " + release + version), ConsoleColor.DarkYellow)});
            Console.ReadLine();
        }
    }

    class Bot
    {

        TwitchClient client;
        HttpClient htpClient = new HttpClient();      // for http requests
        List<Command> comandos = new List<Command>();

        ConcurrentQueue<int> soundQueue;

        List<Tuple<string, int, bool>> soundList;

        public Bot()
        {
            soundQueue = new ConcurrentQueue<int>();
            soundList = Sounds.SoundCollector();
            //comandos
            //comandos.Add(new Croqueta(30 * 1000));
            comandos.Add(new Pitonisio(30 * 1000));
            comandos.Add(new Commandos(60 * 1000));
            comandos.Add(new Hola(60 * 1000));
            comandos.Add(new Azar(60 * 1000));
            comandos.Add(new Traducir(60 * 1000));
            comandos.Add(new Chiste(60 * 1000));
            comandos.Add(new Discord(60 * 1000));
            comandos.Add(new Promo(60 * 1000));
            comandos.Add(new Sounds(soundQueue, soundList));
            comandos.Add(new Vote());
            comandos.Add(new Voice());
            comandos.Add(new Quotes(60 * 1000));
            //comandos.Add(new ####(60 * 1000));

            Thread t = new Thread(() => SoundCommander(soundList, soundQueue));
            t.Start();
            ConnectionCredentials credentials = new ConnectionCredentials(Resource1.nombre_usuario, Resource1.token);
            var clientOptions = new ClientOptions
            {
                MessagesAllowedInPeriod = 750,
                ThrottlingPeriod = TimeSpan.FromSeconds(30)
            };
            WebSocketClient customClient = new WebSocketClient(clientOptions);
            client = new TwitchClient(customClient);
            client.Initialize(credentials, Resource1.nombre_canal);

            client.OnLog += Client_OnLog;
            client.OnRaidNotification += Client_OnRaid;
            client.OnJoinedChannel += Client_OnJoinedChannel;
            client.OnMessageReceived += Client_OnMessageReceived;
            client.OnWhisperReceived += Client_OnWhisperReceived;
            client.OnNewSubscriber += Client_OnNewSubscriber;
            client.OnConnected += Client_OnConnected;

            client.Connect();
        }

        private void SoundCommander(List<Tuple<string, int, bool>> soundList, ConcurrentQueue<int> order)
        {
            var xaudio2 = new XAudio2();
            var masteringVoice = new MasteringVoice(xaudio2);

            int i = 0;

            while(true)
            {
                if(!(order.IsEmpty))
                {

                    order.TryDequeue(out i);
                    order.Clear();

                    try
                    {
                        if (Monitor.TryEnter(soundList[i]))  //if already locked, then it is in cooldown
                        {
                            Sounds.PLaySoundFile(xaudio2, "1) Playing a standard WAV file", Sounds.path + soundList[i].Item1 + ".wav");
                            Monitor.Exit(soundList[i]);
                            new Thread(() =>
                                {
                                    lock (soundList[i])
                                    {
                                        Console.WriteLine("Thread a dormir cooldown");
                                        Thread.Sleep(soundList[i].Item2);
                                        Console.WriteLine("Thread a despertar");
                                    }
                                }).Start();
                            Thread.Sleep(1000);
                        }
                        else
                            Console.WriteLine("Cooldown de "+soundList[i].Item1);


                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Error while playing sounds" + e);
                    }
                }
            }

            masteringVoice.Dispose();
            xaudio2.Dispose();
        }

        private void Client_OnLog(object sender, OnLogArgs e)
        {
            Console.WriteLine($"{e.DateTime.ToString()}: {e.BotUsername} - {e.Data}");
        }

        private void Client_OnRaid(object sender, OnRaidNotificationArgs e)
        {
            Console.WriteLine("I'm being raided");
            client.SendMessage(e.Channel, "Gracias a @" + e.RaidNotification.DisplayName + " por raidearme con " + e.RaidNotification.Badges + "personas");
            //Promote(e.RaidNotification.DisplayName, e.Channel);
        }

        private void Client_OnConnected(object sender, OnConnectedArgs e)
        {
            Console.WriteLine($"Connected to {e.AutoJoinChannel}");
        }

        private void Client_OnJoinedChannel(object sender, OnJoinedChannelArgs e)
        {
            if(Program.debug)
            {
                Console.WriteLine("TwitchBot: Saludating people in chat");
                client.SendMessage(e.Channel, "Hola gente, soy un Bot que habla con el nombre de su amo, qué tal?");
            }
        }

/*
        async private void Azar(string channel, int tries, string[] possibilities)
        {
            string choosenOne = "";
            int data_start = 0;
            int i = 0;
            try
            {
                HttpContent request_headers = new StringContent("{"+
                    "\"jsonrpc\": \"2.0\","+
                    "\"method\": \"generateIntegers\","+
                    "\"params\": {"+
                                    "\"apiKey\": \"686c3196-951d-49a9-a5e4-c08e63d9f8f3\","+
                        "\"n\": 1,"+
                        "\"min\": 0,"+
                        "\"max\": "+(possibilities.Length-1)+","+
                        "\"replacement\": true"+
                    "},"+
                    "\"id\": 42"+
                "}");
                request_headers.Headers.ContentType.MediaType = "application/json"; 
                HttpResponseMessage response = await cliente.PostAsync("https://api.random.org/json-rpc/4/invoke", request_headers);
                Console.WriteLine(await response.Content.ReadAsStringAsync());
                string response_content = await response.Content.ReadAsStringAsync();
                data_start = (response_content.IndexOf("\"data\":[")+ "\"data\":[".Length);

                while(true)
                {
                    if (response_content[data_start + i] == ']')
                        break;
                    choosenOne += response_content[data_start + i];
                    i++;
                }

                Console.WriteLine(choosenOne);
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();
                // Above three lines can be replaced with new helper method below
                // string responseBody = await client.GetStringAsync(uri);

                //Console.WriteLine(possibilities[choosenOne]);
                client.SendMessage(channel, possibilities[int.Parse(choosenOne)]);
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine("\nException Caught!");
                Console.WriteLine("Message :{0} ", e.Message);
            }

        }
*/
        private async void Translate(string channel, string[] args, int initial)
        {
            string translatedOne= "";
            int data_start = 0;
            int i = 0;
            try
            {
                throw new NotSupportedException();
                HttpContent request_headers = new StringContent("{" +
                    "\"q\": \""+args[initial]+"\","+
		            "\"source\": \""+
                    (args.Length < 4 ? "auto\"," : args[initial+2]+"\",")+
		            "\"target\": \""+args[initial+1]+"\""+
                "}");
                request_headers.Headers.ContentType.MediaType = "application/json";
                HttpResponseMessage response = await htpClient.PostAsync("https://libretranslate.com/translate", request_headers);
                Console.WriteLine(await response.Content.ReadAsStringAsync());
                string response_content = await response.Content.ReadAsStringAsync();
                data_start = (response_content.IndexOf("\"translatedText\":") + "\"translatedText\":".Length);

                while (true)
                {
                    if (response_content[data_start + i] == '}')
                        break;
                    translatedOne += response_content[data_start + i];
                    i++;
                }

                Console.WriteLine(translatedOne);
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();
                // Above three lines can be replaced with new helper method below
                // string responseBody = await client.GetStringAsync(uri);

                //Console.WriteLine(possibilities[choosenOne]);
                client.SendMessage(channel, translatedOne);
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine("\nException Caught!");
                Console.WriteLine("Message :{0} ", e.Message);
            }
        }
        // mirar evento recompensas bot twitch
        private void Client_OnMessageReceived(object sender, OnMessageReceivedArgs e)
        {
            string[] aux = null;

            if (e.ChatMessage.Message.Contains("badword"))
                client.TimeoutUser(e.ChatMessage.Channel, e.ChatMessage.Username, TimeSpan.FromMinutes(30), "Bad word! 30 minute timeout!");
            if (e.ChatMessage.Message.StartsWith("!"))
            {
                aux = e.ChatMessage.Message.Substring(1).Split(' ', StringSplitOptions.RemoveEmptyEntries);
                foreach (var item in comandos)
                {
                    if(item.IsCommand(aux[0].ToLower()))
                    {
                        new Thread(() =>
                        {
                            Thread.CurrentThread.IsBackground = true;
                            item.Action(client, e.ChatMessage.Channel, (new Command_Args(aux, 1, e, htpClient)));
                        }).Start();
                    }
                }
            }
        }
        /*
        private void Client_OnMessageReceived_2(object sender, OnMessageReceivedArgs e)
        {
            string[] aux = null;
            List<Command> comandos = new List<Command>();
            comandos.Add(new Croqueta(30 * 1000));

            if (e.ChatMessage.Message.Contains("badword"))
                client.TimeoutUser(e.ChatMessage.Channel, e.ChatMessage.Username, TimeSpan.FromMinutes(30), "Bad word! 30 minute timeout!");
            if (e.ChatMessage.Message.StartsWith("!"))
            {
                aux = e.ChatMessage.Message.Substring(1).Split(' ', StringSplitOptions.RemoveEmptyEntries);
                switch (aux[0].ToLower())
                {
                    case "commandos":
                        Commandos(e.ChatMessage.Channel);
                        break;
                    case "hola":
                        client.SendMessage(e.ChatMessage.Channel, "Hola a ti también @" + e.ChatMessage.Username);
                        break;
                    case "promo":
                        if (aux.Length < 2)
                            Command_error("Promo: ", 0, e.ChatMessage.Channel);
                        else
                            Promote(aux[1], e.ChatMessage.Channel);
                        break;
                    case "grillos":
                    case "niputagracia":
                        //play sounds
                        break;
                    case "translate":
                        if (aux.Length >= 3)
                            Translate(e.ChatMessage.Channel, aux, 1);
                        break;
                    case "croqueta":
                    case "pitonisacroqueta":
                        
                        break;
                    case "cortana":
                        if (aux.Length < 2)
                            Command_error("Cortana: ", 0, e.ChatMessage.Channel);

                        break;
                    case "azar":
                        if (aux.Length != 1)  // quiere escoger los resultados posibles
                        {
                            List<string> aux1 = new List<string>();
                            for (int i = 1; i < aux.Length; i++)
                                aux1.Add(aux[i]);
                            Azar(e.ChatMessage.Channel, 1, aux1.ToArray());
                        }
                        else                  // tirada de moneda tipica
                            Azar(e.ChatMessage.Channel, 1, new string[] { "Cara", "Cruz" });
                        break;
                    case "id":
                        client.SendMessage(e.ChatMessage.Channel, "[Steam|Epic]: " + Resource1.steam);
                        break;
                    case "chiste":
                        pedirChiste(e.ChatMessage.Channel);
                        break;
                    case "discord":
                        client.SendMessage(e.ChatMessage.Channel, "Únete al server de discord en el siguiente enlace ^^:\nhttps://discord.gg/6e5VpDcr");
                        break;
                }
            }
        }
        */

        async private void pedirChiste(string channel)
        {
            try
            {
                HttpResponseMessage response = await htpClient.GetAsync("https://v2.jokeapi.dev/joke/Any?format=txt");
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();
                // Above three lines can be replaced with new helper method below
                // string responseBody = await client.GetStringAsync(uri);

                Console.WriteLine(responseBody);
                client.SendMessage(channel, responseBody);
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine("\nException Caught!");
                Console.WriteLine("Message :{0} ", e.Message);
            }

        }

        async private void Recordatorio(string mensaje, string channel, int milliseconds)
        {
            while(true)
            {
                System.Threading.Thread.Sleep(milliseconds);
                client.SendMessage(channel, mensaje);
            }
        }

        private void Coordinador()
        {
            Recordatorio("Por favor, si os está gustando este canal, dadle follow!!", client.JoinedChannels[0].Channel, 5 * 60 * 1000);
            //Recordatorio("Por favor, si os está gustando este canal, dadle follow!!", client.JoinedChannels[0].Channel, 5 * 60 * 1000);   //subs
        }

        private void Client_OnWhisperReceived(object sender, OnWhisperReceivedArgs e)
        {
            if (e.WhisperMessage.Username == "my_friend")
                client.SendWhisper(e.WhisperMessage.Username, "Bot Whisper: Hey! Whispers are so cool!!");
        }

        private void Client_OnNewSubscriber(object sender, OnNewSubscriberArgs e)
        {
            if (e.Subscriber.SubscriptionPlan == SubscriptionPlan.Prime)
                client.SendMessage(e.Channel, $"Welcome {e.Subscriber.DisplayName} to the substers! You just earned INF points! So kind of you to use your Twitch Prime on this channel!");
            else
                client.SendMessage(e.Channel, $"Welcome {e.Subscriber.DisplayName} to the substers! You just earned INF points!");
        }
    }
}
