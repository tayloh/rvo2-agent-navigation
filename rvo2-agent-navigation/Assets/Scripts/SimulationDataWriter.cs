using System.Collections;
using System.Collections.Generic;

using System;
using System.IO;
using System.Globalization;

/// <summary>
/// File writer specialized for writing simulation data.
/// </summary>
public class SimulationDataWriter
{
    // Global path to simulation data
    // Change per user
    private static string s_simulationDataPath = "C:\\Users\\taylo\\Desktop\\masters\\atcg\\Agent Navigation\\implementations\\simulation_data\\";

    private string _filePath;
    private string _filePathAgentsVsTime;

    /// <summary>
    /// Constructs a SimulationDataWriter. Data from the simulation will be written to
    /// a file named UniqueID_Evac_A{#agents}_E{#exits}_{#runs}.txt
    /// </summary>
    /// <param name="numAgents"></param>
    /// <param name="numExits"></param>
    /// <param name="numRuns"></param>
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

    /// <summary>
    /// Writes the specified simulation parameters to the data file.
    /// </summary>
    /// <param name="goalRadius"></param>
    /// <param name="finalGoalY"></param>
    /// <param name="wallWidth"></param>
    /// <param name="wallLength"></param>
    /// <param name="runs"></param>
    /// <param name="numAgents"></param>
    /// <param name="exitWidth"></param>
    /// <param name="distanceBetweenExits"></param>
    /// <param name="numExits"></param>
    /// <param name="simulationTimeStep"></param>
    /// <param name="agentRadius"></param>
    /// <param name="agentMaxSpeed"></param>
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

    /// <summary>
    /// Writes an evacuation time as: "Total time: {time}" to the data file.
    /// </summary>
    /// <param name="time"></param>
    public void WriteEvacuationTime(float time)
    {
        string text = "Total time: " + time.ToString(CultureInfo.InvariantCulture);
        using (StreamWriter sw = new StreamWriter(_filePath, true))
        {
            sw.WriteLine(text);
        }
    }

    /// <summary>
    /// Writes a list of the number of evacuated agents at separate time steps.
    /// Each entry is numEvacuatedAgents is supposed to be one time step apart.
    /// </summary>
    /// <param name="numEvacuatedAgents"></param>
    public void WriteNumAgentsVsTime(List<int> numEvacuatedAgents)
    {
        using (StreamWriter sw = new StreamWriter(_filePathAgentsVsTime, true))
        {
            int i = 0;
            foreach (int count in numEvacuatedAgents)
            {
                if (i < numEvacuatedAgents.Count - 1)
                {
                    sw.Write(count.ToString(CultureInfo.InvariantCulture) + ",");
                }
                else
                {
                    sw.Write(count.ToString(CultureInfo.InvariantCulture));

                }
                i++;
            }
            sw.Write("\n");
        }
    }

    /// <summary>
    /// Gets the global file path of the simulation data file.
    /// </summary>
    public string GetPath()
    {
        return _filePath;
    }
}
