using ClipperLib;
using SvgNest.Models.SvgNest;
using SvgNest.Utils;

namespace SvgNest
{


    public class GeneticAlgorithm
    {
        private SvgNestConfig config;
        private PolygonBounds binBounds;
        private Random _random;

        public GeneticAlgorithm(List<Node> adam, DoublePoint[] binPolygon, SvgNestConfig config)
        {
            this.config = config ?? new SvgNestConfig { populationSize = 10, mutationRate = 10, rotations = 4 };
            this.binBounds = GeometryUtil.getPolygonBounds(binPolygon);
            _random = new Random();

            // population is an array of individuals. Each individual is a object representing the order of insertion and the angle each part is rotated
            var angles = adam.Select(node => this.randomAngle(node.points)).ToList();

            this.population = new List<Individual> { new Individual { placement = adam, rotation = angles } };

            while (this.population.Count < config.populationSize)
            {
                var mutant = this.mutate(this.population[0]);
                this.population.Add(mutant);
            }
        }

        public List<Individual> population { get; set; }


        //        function GeneticAlgorithm(adam, bin, config)
        //        {

        //            this.config = config || { populationSize: 10, mutationRate: 10, rotations: 4 };
        //            this.binBounds = GeometryUtil.getPolygonBounds(bin);

        //            // population is an array of individuals. Each individual is a object representing the order of insertion and the angle each part is rotated
        //            var angles = [];
        //            for (var i = 0; i < adam.Count; i++)
        //            {
        //                angles.push(this.randomAngle(adam[i]));
        //            }

        //            this.population = [{ placement: adam, rotation: angles}];

        //            while (this.population.Count < config.populationSize)
        //            {
        //                var mutant = this.mutate(this.population[0]);
        //                this.population.push(mutant);
        //            }
        //        }

        private List<double> shuffleArray(List<double> array)
        {
            var random = new Random();
            for (var i = array.Count - 1; i > 0; i--)
            {

                int j = (int)Math.Floor(random.NextDouble() * (i + 1));
                var temp = array[i];
                array[i] = array[j];
                array[j] = temp;
            }
            return array;
        }

        // returns a random angle of insertion
        private double randomAngle(List<DoublePoint> part)
        {
            var angleList = new List<double>();
            for (var i = 0; i < Math.Max(this.config.rotations, 1); i++)
            {
                angleList.Add(i * (360 / this.config.rotations));
            }

            angleList = shuffleArray(angleList);

            for (var i = 0; i < angleList.Count; i++)
            {
                var rotatedPart = GeometryUtil.rotatePolygon(part.ToArray(), angleList[i]);

                // don't use obviously bad angles where the part doesn't fit in the bin
                if (rotatedPart.Bounds.Width < this.binBounds.Width && rotatedPart.Bounds.Height < this.binBounds.Height)
                {
                    return angleList[i];
                }
            }

            return 0;
        }

        // returns a mutated individual with the given mutation rate
        private Individual mutate(Individual individual)
        {
            var clone = new Individual
            {
                placement = new List<Node>(individual.placement),
                rotation = new List<double>(individual.rotation)
            };
            for (var i = 0; i < clone.placement.Count; i++)
            {
                var random = new Random();
                var rand = random.NextDouble();
                if (rand < 0.01 * this.config.mutationRate)
                {
                    // swap current part with next part
                    var j = i + 1;

                    if (j < clone.placement.Count)
                    {
                        var temp = clone.placement[i];
                        clone.placement[i] = clone.placement[j];
                        clone.placement[j] = temp;
                    }
                }

                rand = random.NextDouble();
                if (rand < 0.01 * this.config.mutationRate)
                {
                    clone.rotation[i] = this.randomAngle(clone.placement[i].points);
                }
            }

            return clone;
        }

        private bool contains(List<Node> gene, int id)
        {
            for (var i = 0; i < gene.Count; i++)
            {
                if (gene[i].id == id)
                {
                    return true;
                }
            }
            return false;
        }

        // single point crossover
        private List<Individual> mate(Individual male, Individual female)
        {
            var cutpoint = (int)Math.Round(Math.Min(Math.Max(_random.NextDouble(), 0.1), 0.9) * (male.placement.Count - 1));

            var gene1 = male.placement.Take(cutpoint).ToList();
            var rot1 = male.rotation.Take(cutpoint).ToList();

            var gene2 = female.placement.Take(cutpoint).ToList();
            var rot2 = female.rotation.Take(cutpoint).ToList();

            for (var i = 0; i < female.placement.Count; i++)
            {
                if (!contains(gene1, female.placement[i].id))
                {
                    gene1.Add(female.placement[i]);
                    rot1.Add(female.rotation[i]);
                }
            }

            for (var i = 0; i < male.placement.Count; i++)
            {
                if (!contains(gene2, male.placement[i].id))
                {
                    gene2.Add(male.placement[i]);
                    rot2.Add(male.rotation[i]);
                }
            }

            return new List<Individual> {
                new Individual { placement= gene1, rotation= rot1},
                new Individual{ placement= gene2, rotation= rot2}
            };
        }

        public void generation()
        {

            // Individuals with higher fitness are more likely to be selected for mating
            this.population.Sort((a, b) =>
            {
                return a.fitness - b.fitness;
            });

            // fittest individual is preserved in the new generation (elitism)
            var newpopulation = new List<Individual> { this.population[0] };

            while (newpopulation.Count < this.population.Count)
            {
                var male = this.randomWeightedIndividual();
                var female = this.randomWeightedIndividual(male);

                // each mating produces two children
                var children = this.mate(male, female);

                // slightly mutate children
                newpopulation.Add(this.mutate(children[0]));

                if (newpopulation.Count < this.population.Count)
                {
                    newpopulation.Add(this.mutate(children[1]));
                }
            }

            this.population = newpopulation;
        }

        // returns a random individual from the population, weighted to the front of the list (lower fitness value is more likely to be selected)
        private Individual randomWeightedIndividual(Individual exclude = null)
        {
            var pop = new List<Individual>(this.population);

            if (exclude != null && pop.IndexOf(exclude) >= 0)
            {
                pop.RemoveRange(pop.IndexOf(exclude), 1);
            }

            var rand = _random.NextDouble();

            var lower = 0;
            var weight = 1 / pop.Count;
            var upper = weight;

            for (var i = 0; i < pop.Count; i++)
            {
                // if the random number falls between lower and upper bounds, select this individual
                if (rand > lower && rand < upper)
                {
                    return pop[i];
                }
                lower = upper;
                upper += 2 * weight * ((pop.Count - i) / pop.Count);
            }

            return pop[0];
        }
    }

    public class Individual
    {
        public List<Node> placement { get; set; }
        public List<double> rotation { get; set; }
        public double fitness { get; set; }
    }
}
