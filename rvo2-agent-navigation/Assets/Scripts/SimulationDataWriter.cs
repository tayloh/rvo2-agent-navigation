using System.Collections;
using System.Collections.Generic;

using System;
using System.IO;
using System.Globalization;

public class SimulationDataWriter
{
    // Global path to simulation data
    // Change per user
    private static string s_simulationDataPath = "C:\\Users\\taylo\\Desktop\\masters\\atcg\\Agent Navigation\\implementations\\simulation_data\\";

    private string _filePath;
    private string _filePathAgentsVsTime;

    public SimulationDataWriter(int numAgents, int numExits, int numRuns)
    {
        _filePath = s_simulationDataPath + DateTime.Now.Ticks + "_Evac" + "_A" + numAgents + "_E" + numExits + "_R" + numRuns + ".txt";
        _filePathAgentsVsTime = _filePath + ".extra";

        ResetFileContents();
    }

    private void ResetFileContents()
    {
        using (StreamWriter sw = new StreamWriter(_filePath, false))
        {
            sw.Write(string.Empty);
        }

        using (StreamWriter sw = new StreamWriter(_filePathAgentsVsTime, false))
        {
            sw.Write(string.Empty);
        }
    }

    public void WriteSimulationParameters(float goalRadius, float finalGoalY, float wallWidth, float wallLength,
        int runs, int numAgents, float exitWidth, float distanceBetweenExits, int numExits, float simulationTimeStep,
        float agentRadius, float agentMaxSpeed)
    {
        string parametersText = "";
        parametersText += "GoalRadius:" + goalRadius.ToString(CultureInfo.InvariantCulture) + ",";
        parametersText += "FinalGoalY:" + finalGoalY.ToString(CultureInfo.InvariantCulture) + ",";
        parametersText += "WallWidth:" + wallWidth.ToString(CultureInfo.InvariantCulture) + ",";
        parametersText += "WallLength:" + wallLength.ToString(CultureInfo.InvariantCulture) + ",";
        parametersText += "Runs:" + runs.ToString(CultureInfo.InvariantCulture) + ",";
        parametersText += "NumAgents:" + numAgents.ToString(CultureInfo.InvariantCulture) + ",";
        parametersText += "ExitWidth:" + exitWidth.ToString(CultureInfo.InvariantCulture) + ",";
        parametersText += "DistanceBetweenExits:" + distanceBetweenExits.ToString(CultureInfo.InvariantCulture) + ",";
        parametersText += "NumExits:" + numExits.ToString(CultureInfo.InvariantCulture) + ",";
        parametersText += "SimulationTimeStep:" + simulationTimeStep.ToString(CultureInfo.InvariantCulture) + ",";
        parametersText += "AgentRadius:" + agentRadius.ToString(CultureInfo.InvariantCulture) + ",";
        parametersText += "AgentMaxSpeed:" + agentMaxSpeed.ToString(CultureInfo.InvariantCulture);

        using (StreamWriter sw = new StreamWriter(_filePath, true))
        {
            sw.WriteLine(parametersText);
        }
    }

    public void WriteEvacuationTime(float time)
    {
        string text = "Total time: " + time.ToString(CultureInfo.InvariantCulture);
        using (StreamWriter sw = new StreamWriter(_filePath, true))
        {
            sw.WriteLine(text);
        }
    }

    public void WriteNumAgentsVsTime(List<int> numEvacuatedAgents)
    {
        using (StreamWriter sw = new StreamWriter(_filePathAgentsVsTime, true))
        {
            foreach (int count in numEvacuatedAgents)
            {
                sw.WriteLine(count.ToString(CultureInfo.InvariantCulture));
            }
            sw.Write("\n");
        }
    }

    public string GetPath()
    {
        return _filePath;
    }
}
