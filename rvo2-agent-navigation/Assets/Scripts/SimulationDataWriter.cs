using System.Collections;
using System.Collections.Generic;

using System.IO;
using System.Globalization;

public class SimulationDataWriter
{

    private static string s_simulationDataPath = "C:\\Users\\taylo\\Desktop\\masters\\atcg\\Agent Navigation\\implementations\\simulation_data\\";

    private string _filePath;

    public SimulationDataWriter(int numAgents, int numExits, int numRuns)
    {
        _filePath = s_simulationDataPath + "Evac" + "_A" + numAgents + "_E" + numExits + "_R" + numRuns + ".txt";
    }

    public void WriteEvacuationTime(float time)
    {
        string text = "Total time: " + time.ToString(CultureInfo.InvariantCulture);
        using (StreamWriter sw = new StreamWriter(_filePath, true))
        {
            sw.WriteLine(text);
        }
    }

    public string GetPath()
    {
        return _filePath;
    }
}
