using System;
using System.Collections.Generic;
using System.Linq;
using GaSchedule.Model;

/*
 * X. -S. Yang and Suash Deb, "Cuckoo Search via Lévy flights,"
 * 2009 World Congress on Nature & Biologically Inspired Computing (NaBIC), Coimbatore, India,
 * 2009, pp. 210-214, doi: 10.1109/NABIC.2009.5393690.
 * Copyright (c) 2023 - 2024 Miller Cy Chan
 */

namespace GaSchedule.Algorithm
{
	/****************** Cuckoo Search Optimization (CSO) **********************/
	public class Cso<T> : NsgaIII<T> where T : Chromosome<T>
	{
		private int _max_iterations = 5000;
		private int _chromlen;
		private double _pa;
		private float[] _gBest = null;
		private float[][] _current_position = null;
		private LévyFlights<T> _lf;

		// Initializes Cuckoo Search Optimization
		public Cso(T prototype, int numberOfCrossoverPoints = 2, int mutationSize = 2, float crossoverProbability = 80, float mutationProbability = 3) : base(prototype, numberOfCrossoverPoints, mutationSize, crossoverProbability, mutationProbability)
		{
			// there should be at least 5 chromosomes in population
			if (_populationSize < 5)
				_populationSize = 5;

			_pa = .25;
		}

		static E[][] CreateArray<E>(int rows, int cols)
		{
			E[][] array = new E[rows][];
			for (int i = 0; i < array.GetLength(0); i++)
				array[i] = new E[cols];

			return array;
		}

		protected override void Initialize(List<T> population)
		{
			for (int i = 0; i < _populationSize; ++i) {
				List<float> positions = new();

				// initialize new population with chromosomes randomly built using prototype
				population.Add(_prototype.MakeNewFromPrototype(positions));

				if(i < 1) {
					_chromlen = positions.Count;
					_current_position = CreateArray<float>(_populationSize, _chromlen);
					_lf = new LévyFlights<T>(_chromlen);
				}
			}
		}


		private void UpdateVelocities(List<T> population)
		{
			var current_position = _current_position.ToArray();
			for (int i = 0; i < _populationSize; ++i) {
				var changed = false;
				for (int j = 0; j < _chromlen; ++j) {
					var r = Configuration.Random();
					if(r < _pa) {
						changed = true;
						int d1 = Configuration.Rand(5);
						int d2;
						do {
							d2 = Configuration.Rand(5);
						} while(d1 == d2);
						_current_position[i][j] += (float) (Configuration.Random() * (current_position[d1][j] - current_position[d2][j]));
					}
				}

				if(changed)
					_current_position[i] = _lf.Optimum(_current_position[i], population[i]);
			}
		}

		protected override void Reform()
		{
			Configuration.Seed();
			if (_crossoverProbability < 95)
				_crossoverProbability += 1.0f;
			else if (_pa < .5)
				_pa += .01;
		}

		protected override List<T> Replacement(List<T> population)
		{
			_gBest = _lf.UpdatePositions(population, _populationSize, _current_position, _gBest);
			UpdateVelocities(population);
			
			for (int i = 0; i < _populationSize; ++i) {
				var chromosome = _prototype.MakeEmptyFromPrototype();
				chromosome.UpdatePositions(_current_position[i]);
				population[i] = chromosome;
			}

			return base.Replacement(population);
		}

		// Starts and executes algorithm
		public override void Run(int maxRepeat = 9999, double minFitness = 0.999)
		{
			Console.WriteLine("Starting schedule algorithm...");

			if (_prototype == null)
				return;

			var pop = new List<T>[2];
			pop[0] = new List<T>();
			Initialize(pop[0]);

			// Current generation
			int currentGeneration = 0;
			int bestNotEnhance = 0;
			double lastBestFit = 0.0;

			int cur = 0, next = 1;
			while(currentGeneration < _max_iterations)
			{
				var best = Result;
				if (currentGeneration > 0)
				{
					var status = string.Format("\rFitness: {0:F6}\t Generation: {1}", best.Fitness, currentGeneration);
					Console.Write(status);

					// algorithm has reached criteria?
					if (best.Fitness > minFitness)
						break;

					var difference = Math.Abs(best.Fitness - lastBestFit);
					if (difference <= 1e-6)
						++bestNotEnhance;
					else {
						lastBestFit = best.Fitness;
						bestNotEnhance = 0;
					}

					if (bestNotEnhance > (maxRepeat / 100))
						Reform();
				}

				/******************* crossover *****************/
				var offspring = Crossing(pop[cur]);

				/******************* mutation *****************/
				foreach (var child in offspring)
					child.Mutation(_mutationSize, _mutationProbability);

				pop[cur].AddRange(offspring);

				/******************* replacement *****************/
				pop[next] = Replacement(pop[cur]);
				_best = pop[next][0].Dominates(pop[cur][0]) ? pop[next][0] : pop[cur][0];

				(cur, next) = (next, cur);
				++currentGeneration;
			}
		}

		public override string ToString()
		{
			return "Cuckoo Search Optimization (CSO)";
		}
	}
}
