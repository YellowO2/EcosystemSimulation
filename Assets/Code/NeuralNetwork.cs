using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class NeuralNetworkData
{
    public int[] layers;
    public float[] weights; // Back to a flattened 1D array to work with JSON
    public float[] biases;
}

public class NeuralNetwork
{
    public int[] layers;
    private float[][] neurons;
    private float[][][] weights;
    private float[] biases;

    public NeuralNetwork(int[] layers)
    {
        this.layers = layers;
        InitNeurons();
        InitWeightsAndBiases();
    }

    public NeuralNetwork(NeuralNetwork copy)
    {
        this.layers = new int[copy.layers.Length];
        Array.Copy(copy.layers, this.layers, copy.layers.Length);
        InitNeurons();
        InitWeightsAndBiases();
        CopyWeightsAndBiases(copy.weights, copy.biases);
    }

    // This flattens the 3D weights array into a 1D array for saving.
    public NeuralNetworkData GetData()
    {
        List<float> flatWeights = new List<float>();
        for (int i = 0; i < weights.Length; i++)
            for (int j = 0; j < weights[i].Length; j++)
                for (int k = 0; k < weights[i][j].Length; k++)
                    flatWeights.Add(weights[i][j][k]);

        return new NeuralNetworkData
        {
            layers = this.layers,
            weights = flatWeights.ToArray(),
            biases = this.biases
        };
    }

    // This is the new, corrected loading function.
    public void LoadAndTransferData(NeuralNetworkData data)
    {
        // Copy biases that exist in both the old and new network
        int biasCount = Math.Min(this.biases.Length, data.biases.Length);
        for (int i = 0; i < biasCount; i++)
        {
            this.biases[i] = data.biases[i];
        }

        // Copy weights that exist in both networks
        int oldWeightIndex = 0;
        // Iterate through the connections defined by the OLD network's structure (data.layers)
        for (int i = 1; i < data.layers.Length; i++)
        {
            for (int j = 0; j < data.layers[i]; j++)
            {
                for (int k = 0; k < data.layers[i - 1]; k++)
                {
                    // If this connection also exists in the NEW network's structure, copy the weight.
                    if (i < this.layers.Length && j < this.layers[i] && k < this.layers[i - 1])
                    {
                        this.weights[i - 1][j][k] = data.weights[oldWeightIndex];
                    }
                    oldWeightIndex++; // Always advance, as we're reading sequentially from the old data.
                }
            }
        }
    }

    private void InitNeurons()
    {
        List<float[]> neuronsList = new List<float[]>();
        for (int i = 0; i < layers.Length; i++)
        {
            neuronsList.Add(new float[layers[i]]);
        }
        neurons = neuronsList.ToArray();
    }

    private void InitWeightsAndBiases()
    {
        List<float[][]> weightsList = new List<float[][]>();
        List<float> biasesList = new List<float>();

        for (int i = 1; i < layers.Length; i++)
        {
            List<float[]> layerWeightsList = new List<float[]>();
            int neuronsInPreviousLayer = layers[i - 1];

            for (int j = 0; j < neurons[i].Length; j++)
            {
                biasesList.Add(UnityEngine.Random.Range(-0.5f, 0.5f));
                float[] neuronWeights = new float[neuronsInPreviousLayer];
                for (int k = 0; k < neuronsInPreviousLayer; k++)
                {
                    neuronWeights[k] = UnityEngine.Random.Range(-0.5f, 0.5f);
                }
                layerWeightsList.Add(neuronWeights);
            }
            weightsList.Add(layerWeightsList.ToArray());
        }
        weights = weightsList.ToArray();
        biases = biasesList.ToArray();
    }

    public float[] FeedForward(float[] inputs)
    {
        for (int i = 0; i < inputs.Length; i++)
        {
            neurons[0][i] = inputs[i];
        }

        int biasIndex = 0;
        for (int i = 1; i < layers.Length; i++)
        {
            for (int j = 0; j < neurons[i].Length; j++)
            {
                float value = 0f;
                for (int k = 0; k < neurons[i - 1].Length; k++)
                {
                    value += weights[i - 1][j][k] * neurons[i - 1][k];
                }
                neurons[i][j] = (float)Math.Tanh(value + biases[biasIndex++]);
            }
        }
        return neurons[neurons.Length - 1];
    }

    public static NeuralNetwork Crossover(NeuralNetwork parentA, NeuralNetwork parentB)
    {
        // Create a new child with the same architecture, but random initial weights/biases
        NeuralNetwork child = new NeuralNetwork(parentA.layers);

        // Iterate through each weight and randomly assign it from one of the parents
        for (int i = 0; i < child.weights.Length; i++)
        {
            for (int j = 0; j < child.weights[i].Length; j++)
            {
                for (int k = 0; k < child.weights[i][j].Length; k++)
                {
                    child.weights[i][j][k] = UnityEngine.Random.value < 0.5f ?
                        parentA.weights[i][j][k] :
                        parentB.weights[i][j][k];
                }
            }
        }

        // Iterate through each bias and randomly assign it from one of the parents
        for (int i = 0; i < child.biases.Length; i++)
        {
            child.biases[i] = UnityEngine.Random.value < 0.5f ?
                parentA.biases[i] :
                parentB.biases[i];
        }
        
        return child;
    }
    
    public void Mutate(float chance, float val)
    {
        for (int i = 0; i < biases.Length; i++)
            if (UnityEngine.Random.Range(0f, 1f) < chance)
                biases[i] += UnityEngine.Random.Range(-val, val);

        for (int i = 0; i < weights.Length; i++)
            for (int j = 0; j < weights[i].Length; j++)
                for (int k = 0; k < weights[i][j].Length; k++)
                    if (UnityEngine.Random.Range(0f, 1f) < chance)
                        weights[i][j][k] += UnityEngine.Random.Range(-val, val);
    }

    private void CopyWeightsAndBiases(float[][][] copyWeights, float[] copyBiases)
    {
        for (int i = 0; i < biases.Length; i++) biases[i] = copyBiases[i];
        for (int i = 0; i < weights.Length; i++)
            for (int j = 0; j < weights[i].Length; j++)
                for (int k = 0; k < weights[i][j].Length; k++)
                    weights[i][j][k] = copyWeights[i][j][k];
    }
}