using System;
using System.Collections.Generic;
using System.Linq; // Needed for .Sum()

[System.Serializable]
public class NeuralNetworkData
{
    public int[] layers;
    // We will "flatten" the 3D weights array into a simple 1D array that JSON can handle
    public float[] weights;
    public float[] biases;
}

public class NeuralNetwork
{
    private int[] layers;
    private float[][] neurons;
    private float[][][] weights;
    private float[] biases;

    public NeuralNetwork(int[] layers)
    {
        this.layers = new int[layers.Length];
        for (int i = 0; i < layers.Length; i++)
        {
            this.layers[i] = layers[i];
        }

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

    // --- SAVE & LOAD ---
    public NeuralNetworkData GetData()
    {
        NeuralNetworkData data = new NeuralNetworkData();
        data.layers = this.layers;
        data.biases = this.biases;

        // Flatten the 3D weights array into a 1D array
        List<float> flatWeights = new List<float>();
        for (int i = 0; i < weights.Length; i++)
            for (int j = 0; j < weights[i].Length; j++)
                for (int k = 0; k < weights[i][j].Length; k++)
                    flatWeights.Add(weights[i][j][k]);
        
        data.weights = flatWeights.ToArray();
        return data;
    }

    public void LoadData(NeuralNetworkData data)
    {
        // This is safe because biases is a simple array
        for (int i = 0; i < data.biases.Length; i++)
        {
            this.biases[i] = data.biases[i];
        }

        // "Un-flatten" the 1D weights array back into our 3D structure
        int weightIndex = 0;
        for (int i = 0; i < weights.Length; i++)
        {
            for (int j = 0; j < weights[i].Length; j++)
            {
                for (int k = 0; k < weights[i][j].Length; k++)
                {
                    this.weights[i][j][k] = data.weights[weightIndex++];
                }
            }
        }
    }

    // --- CORE NN LOGIC ---

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

    public void Mutate(float chance, float val)
    {
        for (int i = 0; i < biases.Length; i++)
        {
            if (UnityEngine.Random.Range(0f, 1f) < chance)
                biases[i] += UnityEngine.Random.Range(-val, val);
        }

        for (int i = 0; i < weights.Length; i++)
        {
            for (int j = 0; j < weights[i].Length; j++)
            {
                for (int k = 0; k < weights[i][j].Length; k++)
                {
                    if (UnityEngine.Random.Range(0f, 1f) < chance)
                        weights[i][j][k] += UnityEngine.Random.Range(-val, val);
                }
            }
        }
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