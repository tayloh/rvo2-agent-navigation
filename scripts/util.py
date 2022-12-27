import os

from scipy.stats import ttest_ind

DATA_DIRECTORY = "./data"

class SimulationDataFileParser:
    """Parses a simulation data file and contains methods
    for retrieving the data.
    """

    NUMEXITS = "NumExits"
    RUNS = "Runs"
    NUMAGENTS = "NumAgents"
    TIMESTEP = "SimulationTimeStep"

    def __init__(self, relativeFilePath):
        self._filePath = relativeFilePath

        self._params_dict = {}
        self._evacuation_times = []

        self._create_parameters_dict()
        self._create_evacuation_times_list()

        self._agent_vs_time_data = []*int(self._params_dict[SimulationDataFileParser.RUNS])
        self._create_agents_vs_time_matrix()

    def _get_extras_file(self):
        return self._filePath + ".extra"
    
    def _create_parameters_dict(self):
        with open(self._filePath) as file:
            params_list = file.readline().split(",")
            for param in params_list:
                key, value_string = param.split(":")
                self._params_dict[key] = float(value_string)

    def _create_evacuation_times_list(self):
        with open(self._filePath) as file:
            skipped_params = False
            for line in file:
                if not skipped_params:
                    # Skip past the parameters
                    skipped_params = True
                    continue

                time = float(line.split(":")[1].strip())
                self._evacuation_times.append(time)

    def _create_agents_vs_time_matrix(self):
        with open(self._get_extras_file()) as file:
            for line in file:
                run_agents_per_step = [float(count) for count in line.split(",")]
                self._agent_vs_time_data.append(run_agents_per_step)
                

    def get_parameters_dict(self):
        """Returns a dict of the simulation parameters.
        """
        return self._params_dict

    def get_evacuation_times(self):
        """Returns a list of evacuation times for each run.
        """
        return self._evacuation_times

    def get_agents_vs_time_data(self):
        """
        Pre: Simulation was run with Record Agents Vs Time toggled.

        Returns a list of lists containing the agent count after
        each simulation timestep, for each run. 
        """
        return self._agent_vs_time_data
    
    def get_parsed_file(self):
        """Returns the path of the simulation file related to this 
        object instance.
        """
        return self._filePath


def _collect_data_files(directory):
    files = []
    for file in os.listdir(directory):
        if not file.endswith(".extra"):
            files.append(directory + "/" + file)
    
    return files

def parse_data_files():
    """Returns a list of SimulationDataParser for each 
    simulation data file in DATA_DIRECTORY.
    """
    parsed_data = []
    for file in _collect_data_files(DATA_DIRECTORY):
        parsed_data.append(SimulationDataFileParser(file))
    
    return parsed_data

def get_simulations_by_parameter_value(parsed_data, parameter, value):
    """
    parsed_data: [SimulationDataParser]
    parameter: SimulationDataParser.PARAMETER,
    value: value to retrieve by 

    Returns a list of SimulationDataParser with parameter==value.
    """
    simulations = []
    for sim_data in parsed_data:
        if sim_data.get_parameters_dict()[parameter] == value:
            simulations.append(sim_data)
    return simulations

def sort_simulations_by_parameter(parsed_data, parameter):
    """
    parsed_data: [SimulationDataParser]
    parameter: SimulationDataParser.PARAMETER

    Returns a list of SimulationDataParser sorted in ascending order
    by parameter.
    """

    return sorted(
        parsed_data, 
        key=lambda d: d.get_parameters_dict()[parameter])

def get_simulations_by_agents_exits(agents, exits):
    """Returns all simulations with the specified number of agent
    and exit counts.
    """
    simulations = parse_data_files()
    agents_filtered = get_simulations_by_parameter_value(
        simulations, 
        SimulationDataFileParser.NUMAGENTS,
        agents)
    both_filtered = get_simulations_by_parameter_value(
        agents_filtered, 
        SimulationDataFileParser.NUMEXITS,
        exits)
    
    return both_filtered

def get_agent_counts(simulations):
    """Returns a list of the used agent counts throughout the 
    given simulations.
    """
    counts = []
    for sim in simulations:
        agent_count = sim.get_parameters_dict()[SimulationDataFileParser.NUMAGENTS]
        if agent_count not in counts:
            counts.append(agent_count)
    return counts

def compute_p_value(null_hyp, alt_hyp):
    return ttest_ind(null_hyp, alt_hyp).pvalue

def mean(numbers):
    return sum(numbers) / len(numbers)

def variance(numbers):
    m = mean(numbers)
    return sum((x - m)**2 for x in numbers) / len(numbers)

def std(numbers):
    return variance(numbers)**(1/2)
