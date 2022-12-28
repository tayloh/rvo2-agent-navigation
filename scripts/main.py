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


def plot_avgevac_times_vs_agent_count_per_exit():
    """Plots the mean evacuation times vs. agent count
    for each number of exits [1, 2, 3, 4]. Shows the 
    standard deviation of each mean as an errorbar.
    """
    simulations = util.parse_data_files()
    
    agents = util.get_agent_counts(simulations)

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

            print(
                num_exits, "exits,", 
                sim.get_parameters_dict()[util.SimulationDataFileParser.NUMAGENTS], "agents",
                "mean:", util.mean(evac_times),
                "std:", util.std(evac_times))

        plt.errorbar(agents, means, stds, 
        label=str(num_exits) + " exits", 
        linestyle="", marker="o", markersize=3)
    
    plt.xticks(agents)
    plt.xlabel("Number of agents")
    plt.ylabel("Avg. evacuation time (s)")
    plt.title("Avg. evacuation times vs. Number of agents")
    plt.legend()
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
    plt.xticks([x for x in x_values if x % 2 == 0])
    plt.xlabel("Run number")
    plt.ylabel("Evacuation time (s)")
    plt.title("Evacuation times per run (agents:" + str(agents) + ", exits:" + str(exits) + ")")
    plt.plot(x_values, evac_times)
    plt.show()


def plot_agents_vs_time(file):
    """Takes a file path to a simulation file and 
    plots the number of evacuated agents vs. simulation
    time. Uses the first run in the file (to be changed).
    """
    simulation = util.SimulationDataFileParser(file)

    agents = simulation.get_parameters_dict()[util.SimulationDataFileParser.NUMAGENTS]
    exits = simulation.get_parameters_dict()[util.SimulationDataFileParser.NUMEXITS]
    timestep = simulation.get_parameters_dict()[util.SimulationDataFileParser.TIMESTEP]

    # Grab the first run for instance
    agents_evaced_each_time_step = simulation.get_agents_vs_time_data()[0]

    x_values = [timestep * i for i in range(len(agents_evaced_each_time_step))]

    plt.xlabel("Time (s)")
    plt.ylabel("Agents evacuated")
    plt.title("Agents evacuated vs. time (agents:" + str(agents) + ", exits:" + str(exits) + ")")
    plt.plot(x_values, simulation.get_agents_vs_time_data()[0])
    plt.show()


def compute_pvalues_for_agent_count(agents):
    """Computes the p-values for the specified agent count.
    E.g. from 1 to 2 exits, 2 to 3 exits, 3 to 4 exits.
    """
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


def print_pvalues(agent_counts):
    """Computes and prints the p-values for the difference between
    each consecutive exit number, for each specified agent count.
    E.g. p-values for 50 agents: from 1 to 2 exits, 2 to 3 exits, 
    and 3 to 4 exits. Then for 100 agents, and so on.
    """
    for count in agent_counts:
        pvalues = compute_pvalues_for_agent_count(count)
        print("p-values for", count, "agents")
        print(pvalues)
        print("______________________")


def main():
    # Remember that every call to parse_data_files() is quite
    # expensive... especially if there's a lot of files
    # Ideally, it should only be called once, so the code above is not that great
    simulations = util.parse_data_files()
    agent_counts = util.get_agent_counts(simulations)

    print_pvalues(agent_counts)
    plot_avgevac_times_vs_agent_count_per_exit()
    #plot_evacuation_times(util.get_simulations_by_agents_exits(150, 1)[0].get_parsed_file())
    #plot_agents_vs_time(util.get_simulations_by_agents_exits(100, 3)[0].get_parsed_file())
    
    
    

if __name__ == "__main__":
    main()