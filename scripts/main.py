import util

# Takes time to import these
# import numpy as np
# import scipy.stats as st
# import matplotlib.pyplot as plt

def main():
    parsed_data = util.parse_data_files()
    
    for sim in parsed_data:
        print(sim.get_parameters_dict())
        print(sim.get_agents_vs_time_data()[0])
        print(sim.get_parameters_dict()[util.SimulationDataFileParser.RUNS])

    # TODO: I need one line per number of exits [1, 2, 3, 4]
    # TODO: One line is- x-axis: agents count, y-axis: avg +-std
    # https://stackoverflow.com/questions/22481854/plot-mean-and-standard-deviation
    
    # plt.plot(parsed_data[0].get_agents_vs_time_data()[0])
    # plt.show()
    #plt.plot(parser.get_evacuation_times())
    #plt.show()

if __name__ == "__main__":
    main()