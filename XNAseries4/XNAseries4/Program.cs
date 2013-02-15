using System;

namespace XNASeries4
{
#if WINDOWS || XBOX
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            using (WaterSim game = new WaterSim())
            {
                game.Run();
            }
        }
    }
#endif
}

