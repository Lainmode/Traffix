

using System.Drawing;
using System.Runtime.InteropServices;

namespace Traffix
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            TrafficLights trafficLights1 = new TrafficLights(new GreenLED(), new RedLED(), new YellowLED());
            TrafficLights trafficLights2 = new TrafficLights(new GreenLED(), new RedLED(), new YellowLED());
            TrafficLights trafficLights3 = new TrafficLights(new GreenLED(), new RedLED(), new YellowLED(), allowDuration: 60000);
            TrafficLights trafficLights4 = new TrafficLights(new GreenLED(), new RedLED(), new YellowLED());

            FourWayIntersection intersection = new FourWayIntersection(new List<TrafficLights> { trafficLights1, trafficLights2, trafficLights3, trafficLights4 });

            intersection.Start(30000, 60000);

            Console.ReadLine();
            trafficLights2.GreenLED.TurnOn();
            Console.ReadLine();


        }

        public static void Print(object value)
        {
            Console.WriteLine(value);
        }
    }

    public class FourWayIntersection
    {
        public List<TrafficLights> TrafficLights { get; set; } = new List<TrafficLights>();
        public bool AllowPedestrianCrossing { get; set; } = false;
        public bool IsOperating { get; set; } = false;

        private CancellationTokenSource AlternatingTaskCancellationTokenSource { get; set; }
        private CancellationTokenSource HazardWarningTaskCancellationTokenSource { get; set; }

        public FourWayIntersection(List<TrafficLights> trafficLights)
        {
            TrafficLights = trafficLights;

            AlternatingTaskCancellationTokenSource = new CancellationTokenSource();
            HazardWarningTaskCancellationTokenSource = new CancellationTokenSource();
        }


        public async Task StartHazardWarning()
        {
            TryCancelToken(AlternatingTaskCancellationTokenSource);

            IsOperating = false;

            TrafficLights.ForEach(e => e.TurnAllOff());

            while (!IsOperating)
            {
                foreach (var trafficLight in TrafficLights)
                {
                    Program.Print(TrafficLights.IndexOf(trafficLight) + ": ");

                    trafficLight.GreenLED.TurnOff();
                    trafficLight.YellowLED.TurnOff();

                    trafficLight.RedLED.Toggle();
                    Console.WriteLine();

                }

                await Task.Delay(2000);
            }
        }

        public void Start(int vehicleDuration, int pedestrianDuration)
        {
            Task.Run(() => StartAlternating(vehicleDuration, pedestrianDuration), AlternatingTaskCancellationTokenSource.Token);
        }

        public async Task StartAlternating(int vehicleDuration, int pedestrianDuration)
        {
            TryCancelToken(HazardWarningTaskCancellationTokenSource);
            ResetAllLights();

            Console.Clear();

            Task watcher = Task.Run(() => StartWatcher());

            IsOperating = true;


            while (IsOperating)
            {
                foreach (var trafficLight in TrafficLights)
                {
                    Program.Print(TrafficLights.IndexOf(trafficLight) + ": ");

                    await trafficLight.ChangeToGreen(2000);
                    await Task.Delay(trafficLight.AllowDuration ?? vehicleDuration);
                    await trafficLight.ChangeToRed(5000);
                    await Task.Delay(5000);
                    Console.WriteLine();
                }
            }
        }

        public void ResetAllLights()
        {
            foreach (var trafficLight in TrafficLights)
            {
                trafficLight.TurnAllOff();
                trafficLight.IsOperating = true;
            }
        }

        public async Task StartWatcher()
        {
            while (true)
            {

                if (TrafficLights.Where(e => e.YellowLED.IsOn || e.GreenLED.IsOn).Count() > 1)
                {
                    Program.Print("HAZARD!");
                    HazardWarningTaskCancellationTokenSource = new CancellationTokenSource();
                    Task hazardWarning = Task.Run(() => StartHazardWarning(), HazardWarningTaskCancellationTokenSource.Token);
                    return;
                }

                await Task.Delay(100);
            }
        }

        public void TryCancelToken(CancellationTokenSource cancellationTokenSource)
        {
            try
            {
                if (!cancellationTokenSource.IsCancellationRequested)
                {
                    cancellationTokenSource.Cancel();
                }
            }

            catch (Exception e)
            {
                Program.Print(e.Message);
            }

        }
    }

    public class TrafficLights
    {
        public bool IsOperating { get; set; }
        public GreenLED GreenLED { get; set; }
        public RedLED RedLED { get; set; }
        public YellowLED YellowLED { get; set; }
        public int? AllowDuration { get; set; }

        public TrafficLights(GreenLED greenLED, RedLED redLED, YellowLED yellowLED, int? allowDuration = null)
        {
            this.GreenLED = greenLED;
            this.RedLED = redLED;
            this.YellowLED = yellowLED;
            this.AllowDuration = allowDuration;
        }

        public async Task ChangeToRed(int duration)
        {
            if (!IsOperating)
            {
                throw new Exception("TRAFFIC LIGHTS ARE OFF!");
            }

            GreenLED.TurnOff();
            YellowLED.TurnOn();

            await Task.Delay(duration);

            YellowLED.TurnOff();
            RedLED.TurnOn();
        }

        public async Task ChangeToGreen(int duration)
        {
            if (!IsOperating)
            {
                throw new Exception("TRAFFIC LIGHTS ARE OFF!");
            }

            YellowLED.TurnOn();

            await Task.Delay(duration);

            YellowLED.TurnOff();
            RedLED.TurnOff();
            GreenLED.TurnOn();
        }

        public void TurnAllOff()
        {
            YellowLED.TurnOff();
            RedLED.TurnOff();
            GreenLED.TurnOff();
        }

        public void TurnAllOn()
        {
            YellowLED.TurnOn();
            RedLED.TurnOn();
            GreenLED.TurnOn();
        }
    }

    public class LED
    {
        public int HexValue { get; set; }
        public string? ColorName { get; set; }
        public bool IsOn { get; set; }

        public void Toggle()
        {
            if (IsOn)
            {
                TurnOff();
            }

            else
            {
                TurnOn();
            }
        }

        public void TurnOff()
        {
            IsOn = false;
            Program.Print($"Turned Off {ColorName}");
        }

        public void TurnOn()
        {
            IsOn = true;
            Program.Print($"Turned On {ColorName}");
        }
    }

    public class GreenLED : LED
    {
        public GreenLED()
        {
            HexValue = 0x008000;
            ColorName = "GREEN";
        }
    }
    public class RedLED : LED
    {
        public RedLED()
        {
            HexValue = 0xFF0000;
            ColorName = "RED";
        }
    }
    public class YellowLED : LED
    {
        public YellowLED()
        {
            HexValue = 0xFFFF00;
            ColorName = "YELLOW";
        }
    }

    public class RGBLED
    {

    }

}