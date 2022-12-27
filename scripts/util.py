import os

DATA_DIRECTORY = "./data"

class SimulationDataFileParser:

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
        return self._params_dict

    def get_evacuation_times(self):
        return self._evacuation_times

    def get_agents_vs_time_data(self):
        return self._agent_vs_time_data
    
    def get_parsed_file(self):
        return self._filePath


def _collect_data_files(directory):
    files = []
    for file in os.listdir(directory):
        if not file.endswith(".extra"):
            files.append(directory + "/" + file)
    
    return files

def parse_data_files():
    parsed_data = []
    for file in _collect_data_files(DATA_DIRECTORY):
        parsed_data.append(SimulationDataFileParser(file))
    
    return parsed_data
