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


def plot_multiplicative_change_in_evac_time():

    # Values from printed output in 
    # plot_avgevac_times_vs_agent_count_per_exit()
    
    plt.figure(figsize=(9,9))

    exits_1to2 = [1.0481646509654148, 1.2729222520107237, 2.12400875034181, 3.238745822685276, 
    3.5070577856197613, 3.3390993959362985, 3.37902397260274, 3.1874004845967465, 
    2.913927248715225, 2.8639930469332007, 2.6729021572957365]

    exits_2to3 = [1.0068361461226234, 1.0830429732868758, 1.2375634517766498, 1.4630428530342248, 
    1.5873497490955772, 1.7691635091809965, 1.9299405155320555, 1.9900805951642901, 
    2.1352991662579694, 2.149108762941616, 2.2763787519938132]

    exits_3to4 = [0.9803141361256545, 0.9963355834136932, 1.04805816634155, 1.0673829623944744, 
    1.1319682959048876, 1.1776887871853547, 1.2211460855528653, 1.2587358016127632, 
    1.2539975399753998, 1.2462090981644054, 1.2780454657771187]

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

    exits_1to2 = [0.567499999999999, 3.817499999999999, 20.5525, 56.942499999999995, 85.2525, 
    106.48749999999998, 138.935, 157.985, 166.66000000000003, 187.65749999999997, 196.96749999999997]

    exits_2to3 = [0.08000000000000007, 1.0725000000000016, 3.51, 8.049999999999997, 12.582500000000003, 
    19.792499999999997, 28.139999999999997, 35.9325, 46.2975, 53.83, 66.0175]

    exits_3to4 = [-0.23499999999999943, -0.04750000000000121, 0.6775000000000002, 1.0975000000000001, 
    2.4974999999999987, 3.8825000000000003, 5.48, 7.459999999999997, 8.259999999999998, 9.254999999999995, 
    11.252499999999998]


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
    #plot_evacuation_times(util.get_simulations_by_agents_exits(40, 3)[0].get_parsed_file())
    #plot_evacuation_times_hist(util.get_simulations_by_agents_exits(40, 4)[0].get_parsed_file())
    #plot_agents_vs_time(util.get_simulations_by_agents_exits(40, 4)[0].get_parsed_file(),
    #mode="median")

    #plot_multiplicative_change_in_evac_time()
    #plot_change_in_evac_time()
    

if __name__ == "__main__":
    main()