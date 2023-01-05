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
        mult_changes_in_evacuation_time = []
        changes_in_evacuation_time = []    
        avg_of_increases = []
        
        # Number of means arrays saved is number of exits
        for i in range(1, len(means_saved)):
            avg_increase = 0
            mult_changes_for_this_nr_exits = []
            changes_for_this_nr_exits = []
            for j in range(len(means_saved[0])):
                # One more exit vs one less exit
                mult_change = means_saved[i-1][j] / means_saved[i][j]
                change = means_saved[i-1][j] - means_saved[i][j]
                avg_increase += mult_change
                mult_changes_for_this_nr_exits.append(mult_change)
                changes_for_this_nr_exits.append(change)

            avg_increase /= len(means_saved[0])
            avg_of_increases.append(avg_increase)
            mult_changes_in_evacuation_time.append(mult_changes_for_this_nr_exits)
            changes_in_evacuation_time.append(changes_for_this_nr_exits)

        print("Avg. of change (over agent counts) for each nr. of exits:", avg_of_increases)
        print("Exact mult. change per agent count, for each nr. of exits:", mult_changes_in_evacuation_time)
        print("Exact change per agent count, for each nr. of exits:", changes_in_evacuation_time)

    plt.xticks(agents)
    plt.xlabel("Number of agents")
    plt.ylabel("Avg. evacuation time (s)")
    plt.title("Avg. evacuation time vs. Number of agents")
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
    time. Specify mode: median, slowest, fastest.
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
    plt.title("Agents evacuated vs. Time (agents:" + str(agents) + ", exits:" + str(exits) + ")")
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


def plot_multiplicative_change_in_evac_time():

    # Values from printed output in 
    # plot_avgevac_times_vs_agent_count_per_exit()
    
    plt.figure(figsize=(9,9))

    exits_1to2 = [1.0764248704663213, 1.3669144981412642, 2.0926808749298935, 3.054178790007023, 
    3.5573806717737186, 3.550697830839079, 3.358808847577244, 3.1179729682041146, 
    2.910311200344185, 2.8084619360555267, 2.804493937796521]

    exits_2to3 = [1.020938946440379, 1.0753547871277234, 1.2384094460843895, 1.4191940766054392, 
    1.5900679315999062, 1.7992133924969747, 1.9368228038006334, 2.0647770329350044, 
    2.1380388790090143, 2.2178612369670834, 2.1736930054905703]

    exits_3to4 = [0.9975813544415127, 1.0107070707070707, 1.0549551199853453, 1.105984251968504, 
    1.1715148188803512, 1.1759962049335864, 1.2122865514802466, 1.237399693445136, 
    1.3364202589739387, 1.3276561820553705, 1.3778698769817774]

    agents = [20, 40, 60, 80, 100, 120, 140, 160, 180, 200, 220]

    plt.plot(agents, exits_1to2, 
        label="1 to 2 exits", 
        linestyle="", marker="o", color="orange", markersize=3)

    plt.plot(agents, exits_2to3, 
        label="2 to 3 exits", 
        linestyle="", marker="o", color="green", markersize=3)

    plt.plot(agents, exits_3to4, 
        label="3 to 4 exits", 
        linestyle="", marker="o", color="red", markersize=3)

    plt.xticks(agents)
    plt.xlabel("Number of agents")

    # Remember it is average evacuation time over 100 runs
    plt.ylabel("Factor difference in evacuation time")

    plt.title("Factor difference in evacuation time vs. Number of agents")
    plt.legend(loc=2)
    plt.show()

    
def plot_change_in_evac_time():

    # Values from printed output in 
    # plot_avgevac_times_vs_agent_count_per_exit()
    
    plt.figure(figsize=(9,9))

    exits_1to2 = [0.8849999999999998, 4.935000000000002, 19.4825, 51.185, 86.7975, 113.76750000000001, 
    137.035, 150.0425, 166.5075, 183.69, 205.3875]

    exits_2to3 = [0.2375000000000007, 0.942499999999999, 3.4324999999999974, 7.359999999999999, 
    12.594999999999999, 19.8125, 28.099999999999998, 36.5325, 46.394999999999996, 
    55.775000000000006, 61.457499999999996]

    exits_3to4 = [-0.027499999999999858, 0.13250000000000028, 0.75, 1.682500000000001, 3.125, 
    3.710000000000001, 5.252500000000001, 6.582500000000003, 10.2625, 11.302500000000002, 14.36]


    agents = [20, 40, 60, 80, 100, 120, 140, 160, 180, 200, 220]

    plt.plot(agents, exits_1to2, 
        label="1 to 2 exits", 
        linestyle="", marker="o", color="orange", markersize=3)

    plt.plot(agents, exits_2to3, 
        label="2 to 3 exits", 
        linestyle="", marker="o", color="green", markersize=3)

    plt.plot(agents, exits_3to4, 
        label="3 to 4 exits", 
        linestyle="", marker="o", color="red", markersize=3)

    plt.xticks(agents)
    plt.xlabel("Number of agents")

    # Remember it is average evacuation time over 100 runs
    plt.ylabel("Difference in evacuation time (s)")

    plt.title("Difference in evacuation time vs. Number of agents")
    plt.legend(loc=2)
    plt.show()

def main():
    # Remember that every call to parse_data_files() is quite
    # expensive... especially if there's a lot of files
    # Ideally, it should only be called once, so the code above is not that great
    
    """Print p-values
    """
    print_pvalues()
    
    """Plots
    """
    plot_avgevac_times_vs_agent_count_per_exit(print_data=False)
    #plot_evacuation_times(util.get_simulations_by_agents_exits(220, 4)[0].get_parsed_file())
    #plot_evacuation_times_hist(util.get_simulations_by_agents_exits(220, 4)[0].get_parsed_file())
    #plot_agents_vs_time(util.get_simulations_by_agents_exits(40, 3)[0].get_parsed_file(),
    #mode="median")

    #plot_multiplicative_change_in_evac_time()
    #plot_change_in_evac_time()
    

if __name__ == "__main__":
    main()