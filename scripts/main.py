import util

import matplotlib.pyplot as plt

def example():
    """Example usage of util.
    """

    # Use parse_data_files() once to get all simulations
    # in a workable format as SimulationDataFileParsers
    simulations = util.parse_data_files()

    for num_exits in [1, 2, 3, 4]:

        # Get all simulations by some parameter value,
        # in this case the number of exits
        by_exits = util.get_simulations_by_parameter_value(
            simulations,
            util.SimulationDataFileParser.NUMEXITS,
            num_exits
        )

        # Order the simulations by some parameter,
        # in this case agent count
        ordered_by_agents = util.sort_simulations_by_parameter(
            by_exits,
            util.SimulationDataFileParser.NUMAGENTS
        )
        print(num_exits, " exits")
        for sim in ordered_by_agents:
            # Prints the associated file
            print(sim.get_parsed_file())


def plot_avgevac_times_vs_agent_count_per_exit(print_data=False):
    """Plots the mean evacuation times vs. agent count
    for each number of exits [1, 2, 3, 4]. Shows the 
    standard deviation of each mean as an errorbar.
    """
    simulations = util.parse_data_files()
    
    agents = util.get_agent_counts(simulations)

    means_saved = []
    for num_exits in [1, 2, 3, 4]:
        # One line per exit

        by_exits = util.get_simulations_by_parameter_value(
            simulations,
            util.SimulationDataFileParser.NUMEXITS,
            num_exits
        )
        ordered_by_agents = util.sort_simulations_by_parameter(
            by_exits,
            util.SimulationDataFileParser.NUMAGENTS
        )

        means = []
        stds = []
        for sim in ordered_by_agents:
            evac_times = sim.get_evacuation_times()
            means.append(util.mean(evac_times))
            stds.append(util.std(evac_times))

            if print_data:
                print(
                    num_exits, "exits,", 
                    sim.get_parameters_dict()[util.SimulationDataFileParser.NUMAGENTS], "agents,",
                    "mean:", util.mean(evac_times),
                    "std:", util.std(evac_times))

        means_saved.append(means)
        plt.errorbar(agents, means, stds, 
        label=str(num_exits) + " exits", 
        linestyle="", marker="o", markersize=3)

    # Print the multiplicative decrease going from i-1 to i exits
    if print_data:    
        avg_of_increases = []
        for i in range(1, len(means_saved)):
            avg_increase = 0
            for j in range(len(means_saved[0])):
                # One more exit vs one less exit
                avg_increase += means_saved[i-1][j] / means_saved[i][j]
            avg_increase /= len(means_saved[0])
            avg_of_increases.append(avg_increase)
        print(avg_of_increases)

    plt.xticks(agents)
    plt.xlabel("Number of agents")
    plt.ylabel("Avg. evacuation time (s)")
    plt.title("Avg. evacuation times vs. Number of agents")
    plt.legend(loc=2)
    plt.show()


def plot_evacuation_times(file):
    """Takes a file path to a simulation file and plots the 
    evacuation times for each run in that simulation.
    """
    simulation = util.SimulationDataFileParser(file)

    agents = simulation.get_parameters_dict()[util.SimulationDataFileParser.NUMAGENTS]
    exits = simulation.get_parameters_dict()[util.SimulationDataFileParser.NUMEXITS]

    evac_times = simulation.get_evacuation_times()
    x_values = [i+1 for i in range(len(evac_times))]
    plt.xticks([x for x in x_values if x % 8 == 0])
    plt.xlabel("Run number")
    plt.ylabel("Evacuation time (s)")
    plt.title("Evacuation times per run (agents:" + str(agents) + ", exits:" + str(exits) + ")")
    plt.plot(x_values, evac_times)
    plt.show()


def plot_agents_vs_time(file, mode="median"):
    """Takes a file path to a simulation file and 
    plots the number of evacuated agents vs. simulation
    time. Uses the slowest run (to be changed).
    """
    simulation = util.SimulationDataFileParser(file)

    agents = simulation.get_parameters_dict()[util.SimulationDataFileParser.NUMAGENTS]
    exits = simulation.get_parameters_dict()[util.SimulationDataFileParser.NUMEXITS]
    timestep = simulation.get_parameters_dict()[util.SimulationDataFileParser.TIMESTEP]

    # Grab the first run for instance
    #agents_evaced_each_time_step = simulation.get_agents_vs_time_data()[0]
    if mode == "median":
        agents_evaced_each_time_step = util.get_median_run(simulation)
    elif mode == "slowest":
        agents_evaced_each_time_step = util.get_slowest_run(simulation)
    elif mode == "fastest":
        agents_evaced_each_time_step = util.get_fastest_run(simulation)

    x_values = [timestep * i for i in range(len(agents_evaced_each_time_step))]

    plt.xlabel("Time (s)")
    plt.ylabel("Agents evacuated")
    plt.title("Agents evacuated vs. time (agents:" + str(agents) + ", exits:" + str(exits) + ")")
    plt.plot(x_values, agents_evaced_each_time_step)
    plt.show()

def plot_evacuation_times_hist(file):
    """Takes a file path to a simulation file and plots the 
    evacuation times as a histogram.
    """
    simulation = util.SimulationDataFileParser(file)

    agents = simulation.get_parameters_dict()[util.SimulationDataFileParser.NUMAGENTS]
    exits = simulation.get_parameters_dict()[util.SimulationDataFileParser.NUMEXITS]
    timestep = simulation.get_parameters_dict()[util.SimulationDataFileParser.TIMESTEP]

    # Grab the first run for instance
    #agents_evaced_each_time_step = simulation.get_agents_vs_time_data()[0]
    evac_times = simulation.get_evacuation_times()

    #x_values = [timestep * i for i in range(len(agents_evaced_each_time_step))]

    plt.xlabel("Evacuation time")
    plt.ylabel("Frequency")
    plt.title("Histogram of the evacuation times (agents:" + str(agents) + ", exits:" + str(exits) + ")")
    plt.hist(evac_times, bins=30)
    plt.show()

def compute_pvalues_for_agent_count(agents, simulations=None):
    """Computes the p-values for the specified agent count.
    E.g. from 1 to 2 exits, 2 to 3 exits, 3 to 4 exits.
    """
    if simulations == None:
        simulations = util.parse_data_files()
    
    by_agents = util.get_simulations_by_parameter_value(
        simulations, 
        util.SimulationDataFileParser.NUMAGENTS,
        agents
    )

    sorted_by_exits = util.sort_simulations_by_parameter(
        by_agents,
        util.SimulationDataFileParser.NUMEXITS)

    p_values = []
    for i in range(1, len(sorted_by_exits)):
        null_hyp = sorted_by_exits[i-1].get_evacuation_times()
        alt_hyp = sorted_by_exits[i].get_evacuation_times()
        p_value = util.compute_p_value(null_hyp, alt_hyp)
        p_values.append(p_value)
    
    return p_values


def print_pvalues():
    """Computes and prints the p-values for the difference between
    each consecutive exit number, for each agent count.
    E.g. p-values for 50 agents: from 1 to 2 exits, 2 to 3 exits, 
    and 3 to 4 exits. Then for 100 agents, and so on.
    Returns a 2d list of the p-values as well.
    """
    simulations = util.parse_data_files()
    agent_counts = util.get_agent_counts(simulations)

    p_values_list = []
    for count in agent_counts:
        # Pass in precomputed parsed data such that it is not 
        # recomupted for every agent count
        pvalues = compute_pvalues_for_agent_count(count, simulations=simulations)
        print("p-values for", count, "agents")
        print(pvalues)
        print("______________________")

        p_values_list.append((count, pvalues))

    return p_values_list


def main():
    # Remember that every call to parse_data_files() is quite
    # expensive... especially if there's a lot of files
    # Ideally, it should only be called once, so the code above is not that great
    
    """Print p-values
    """
    print_pvalues()
    
    """Plots
    """
    plot_avgevac_times_vs_agent_count_per_exit()
    #plot_evacuation_times(util.get_simulations_by_agents_exits(40, 3)[0].get_parsed_file())
    #plot_evacuation_times_hist(util.get_simulations_by_agents_exits(40, 4)[0].get_parsed_file())
    #plot_agents_vs_time(util.get_simulations_by_agents_exits(40, 4)[0].get_parsed_file(),
    #mode="median")
    

if __name__ == "__main__":
    main()