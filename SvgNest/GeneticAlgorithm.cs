using ClipperLib;
using SvgNest.Models;
using SvgNest.Models.GeometryUtil;
using SvgNest.Utils;

namespace SvgNest
{
    public class GeneticAlgorithm
    {
        private SvgNestConfig _config;
        private PolygonBounds _binBounds;
        private Random _random;

        public GeneticAlgorithm(List<Node> adam, DoublePoint[] binPolygon, SvgNestConfig config)
        {
            this._config = config ?? new SvgNestConfig { PopulationSize = 10, MutationRate = 10, Rotations = 4 };
            this._binBounds = GeometryUtil.GetPolygonBounds(binPolygon);
            _random = new Random();

            // population is an array of individuals. Each individual is a object representing the order of insertion and the angle each part is rotated
            var angles = adam.Select(node => this.RandomAngle(node.Points)).ToList();

            this.Population = new List<Individual> { new Individual { Placement = adam, Rotation = angles } };

            while (this.Population.Count < config.PopulationSize)
            {
                var mutant = this.Mutate(this.Population[0]);
                this.Population.Add(mutant);
            }
        }

        public List<Individual> Population { get; set; }

        private List<double> ShuffleArray(List<double> array)
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
        private double RandomAngle(List<DoublePoint> part)
        {
            var angleList = new List<double>();
            for (var i = 0; i < Math.Max(this._config.Rotations, 1); i++)
            {
                angleList.Add(i * (360 / this._config.Rotations));
            }

            angleList = ShuffleArray(angleList);

            for (var i = 0; i < angleList.Count; i++)
            {
                var rotatedPart = GeometryUtil.RotatePolygon(part.ToArray(), angleList[i]);

                // don't use obviously bad angles where the part doesn't fit in the bin
                if (rotatedPart.Bounds.Width < this._binBounds.Width && rotatedPart.Bounds.Height < this._binBounds.Height)
                {
                    return angleList[i];
                }
            }

            return 0;
        }

        // returns a mutated individual with the given mutation rate
        private Individual Mutate(Individual individual)
        {
            var clone = new Individual
            {
                Placement = new List<Node>(individual.Placement),
                Rotation = new List<double>(individual.Rotation)
            };
            for (var i = 0; i < clone.Placement.Count; i++)
            {
                var random = new Random();
                var rand = random.NextDouble();
                if (rand < 0.01 * this._config.MutationRate)
                {
                    // swap current part with next part
                    var j = i + 1;

                    if (j < clone.Placement.Count)
                    {
                        var temp = clone.Placement[i];
                        clone.Placement[i] = clone.Placement[j];
                        clone.Placement[j] = temp;
                    }
                }

                rand = random.NextDouble();
                if (rand < 0.01 * this._config.MutationRate)
                {
                    clone.Rotation[i] = this.RandomAngle(clone.Placement[i].Points);
                }
            }

            return clone;
        }

        private bool Contains(List<Node> gene, int id)
        {
            for (var i = 0; i < gene.Count; i++)
            {
                if (gene[i].Id == id)
                {
                    return true;
                }
            }
            return false;
        }

        // single point crossover
        private List<Individual> Mate(Individual male, Individual female)
        {
            var cutpoint = (int)Math.Round(Math.Min(Math.Max(_random.NextDouble(), 0.1), 0.9) * (male.Placement.Count - 1));

            var gene1 = male.Placement.Take(cutpoint).ToList();
            var rot1 = male.Rotation.Take(cutpoint).ToList();

            var gene2 = female.Placement.Take(cutpoint).ToList();
            var rot2 = female.Rotation.Take(cutpoint).ToList();

            for (var i = 0; i < female.Placement.Count; i++)
            {
                if (!Contains(gene1, female.Placement[i].Id))
                {
                    gene1.Add(female.Placement[i]);
                    rot1.Add(female.Rotation[i]);
                }
            }

            for (var i = 0; i < male.Placement.Count; i++)
            {
                if (!Contains(gene2, male.Placement[i].Id))
                {
                    gene2.Add(male.Placement[i]);
                    rot2.Add(male.Rotation[i]);
                }
            }

            return new List<Individual> {
                new Individual { Placement= gene1, Rotation= rot1},
                new Individual{ Placement= gene2, Rotation= rot2}
            };
        }

        public void Generation()
        {
            // Individuals with higher fitness are more likely to be selected for mating
            Population = Population.OrderByDescending(i=>i.Fitness).ToList();

            // fittest individual is preserved in the new generation (elitism)
            var newpopulation = new List<Individual> { this.Population[0] };

            while (newpopulation.Count < this.Population.Count)
            {
                var male = this.RandomWeightedIndividual();
                var female = this.RandomWeightedIndividual(male);

                // each mating produces two children
                var children = this.Mate(male, female);

                // slightly mutate children
                newpopulation.Add(this.Mutate(children[0]));

                if (newpopulation.Count < this.Population.Count)
                {
                    newpopulation.Add(this.Mutate(children[1]));
                }
            }

            this.Population = newpopulation;
        }

        // returns a random individual from the population, weighted to the front of the list (lower fitness value is more likely to be selected)
        private Individual RandomWeightedIndividual(Individual exclude = null)
        {
            var pop = new List<Individual>(this.Population);

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
}
