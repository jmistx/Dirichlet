using System;
using System.Security.Cryptography;

namespace HE.Logic
{
    public class HeatEquationSolver
    {
        public double LeftBoundary { get; set; }
        public double RightBoundary { get; set; }
        public Func<double, double> LeftBoundCondition { get; set; }
        public Func<double, double> RightBoundCondition { get; set; }
        public Func<double, double> StartCondition { get; set; }
        public Func<double, double, double> Function { get; set; }

        public HeatEquationSolver()
        {
            LeftBoundary = 0;
            RightBoundary = 1;
            LeftBoundCondition = t => 0;
            RightBoundCondition = t => 0;
            StartCondition = x => 0;
            Function = (x, t) => 0;
        }

        public EquationSolveAnswer Solve(double timeOfEnd, int spaceIntervals, int timeIntervals)
        {
            var spaceNodesCount = spaceIntervals + 1;
            var timeNodesCount = timeIntervals + 1;

            var answer = new EquationSolveAnswer
            {
                LastLayer = new double[spaceNodesCount],
                Nodes = new double[spaceNodesCount],
                TimeOfEnd = timeOfEnd
            };

            FillNodes(spaceIntervals, spaceNodesCount, answer);

            var previousLayer = new double[spaceNodesCount];
            var currentLayer = new double[spaceNodesCount];

            InitializeFirstLayer(currentLayer, spaceNodesCount, answer);

            var timeStep = (timeOfEnd / timeIntervals);
            var spaceStep = (RightBoundary - LeftBoundary)/spaceIntervals;

            for (int i = 1; i < timeNodesCount; i++)
            {
                var currentTime = i*timeStep;
                SwapLayers(ref currentLayer, ref previousLayer);
                SolveNextLayer(previousLayer, currentLayer, currentTime, answer.Nodes, timeStep, spaceStep);
            }

            SaveCurrentLayer(currentLayer, answer);

            return answer;
        }

        private static void SaveCurrentLayer(double[] currentLayer, EquationSolveAnswer answer)
        {
            for (int i = 0; i < currentLayer.Length; i++)
            {
                answer.LastLayer[i] = currentLayer[i];
            }
        }

        private static void SwapLayers(ref double[] currentLayer, ref double[] previousLayer)
        {
            double[] temp = previousLayer;
            previousLayer = currentLayer;
            currentLayer = temp;
        }

        private void SolveNextLayer(double[] previousLayer, double[] currentLayer, double currentTime, double[] nodes, double timeStep, double spaceStep)
        {
            var spaceNodesCount = currentLayer.Length;

            var k = timeStep;
            var h = spaceStep;

            var innerNodesCount = spaceNodesCount - 2;
            var diagonal = new double[innerNodesCount, 3];
            var rightPart = new double[innerNodesCount];

            {
                diagonal[0, 1] = 1;
                diagonal[0, 2] = (-1) * k / (h * h + 2 * k);
                rightPart[0] = ((k * h * h) / (h * h + 2 * k)) *
                               (1.0 / (h * h) * LeftBoundCondition(currentTime) + previousLayer[1] / k + Function(nodes[1], currentTime));                
            }
            {
                diagonal[spaceNodesCount - 3, 0] = (-1) * k / (h * h + 2 * k);
                diagonal[spaceNodesCount - 3, 1] = 1;
                rightPart[spaceNodesCount - 3] = ((k * h * h) / (h * h + 2 * k)) *
                               (1.0 / (h * h) * RightBoundCondition(currentTime) + previousLayer[spaceNodesCount - 2] / k + Function(nodes[spaceNodesCount - 2], currentTime)); 
            }
            for (int i = 1; i < innerNodesCount - 1; i++)
            {
                diagonal[i, 0] = -1/(h*h);
                diagonal[i, 1] = 1/k + 2/(h*h);
                diagonal[i, 2] = -1/(h*h);
                rightPart[i] = previousLayer[i + 1]/k + Function(nodes[i + 1], currentTime);
            }

            var calculatedLayerValues = SolveSystemOfLinearEquations(diagonal, rightPart);

            currentLayer[0] = LeftBoundCondition(currentTime);
            currentLayer[spaceNodesCount - 1] = RightBoundCondition(currentTime);

            for (int i = 1; i < spaceNodesCount - 1; i++)
            {
                currentLayer[i] = calculatedLayerValues[i - 1];
            }
        }

        public double[] SolveSystemOfLinearEquations(double[,] diagonal, double[] rightPart)
        {
            var n = diagonal.GetLength(0);
            var result = new double[n];
            var cModified = new double[n];
            var fModified = new double[n];
            ValidateMatrix(diagonal, n);

            {
                int i = 0;

                double c = diagonal[i, 1];
                double f = rightPart[i];

                cModified[i] = c;
                fModified[i] = f;
            }

            for (int i = 1; i < n; i++)
            {
                double a = diagonal[i, 0];
                double c = diagonal[i, 1];
                double b = diagonal[i - 1, 2];
                double f = rightPart[i];

                cModified[i] = c - a / cModified[i - 1] * b;
                fModified[i] = f - a * fModified[i - 1] / cModified[i - 1];
            }

            result[n - 1] = fModified[n - 1] / cModified[n - 1];
            for (int i = (n - 1) - 1; i >= 0; i--)
            {
                double b = diagonal[i, 2];
                result[i] = (fModified[i] - b * result[i + 1]) / cModified[i];
            }
            return result;
        }

        private static void ValidateMatrix(double[,] diagonal, int n)
        {
            if (diagonal[0, 0] != 0)
            {
                throw new ArgumentException("A[0] must be 0");
            }
            if (diagonal[n - 1, 2] != 0)
            {
                throw new ArgumentException("B[n-1] must be 0");
            }
        }

        private void InitializeFirstLayer(double[] previousLayer, int spaceNodesCount, EquationSolveAnswer answer)
        {
            for (var i = 0; i < spaceNodesCount; i++)
            {
                previousLayer[i] = StartCondition(answer.Nodes[i]);
            }
        }

        private void FillNodes(int spaceIntervals, int spaceNodesCount, EquationSolveAnswer answer)
        {
            for (var i = 0; i < spaceNodesCount; i++)
            {
                answer.Nodes[i] = LeftBoundary + (RightBoundary*i)/spaceIntervals;
            }
        }
    }
}