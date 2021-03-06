﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Google.Protobuf;
using Microsoft.ML;
using Microsoft.ML.Data;
using System.Threading.Tasks;

using System.Diagnostics;
using Microsoft.ML.Model;
using Microsoft.ML.Trainers;
using System.Collections.Immutable;

namespace MLDotNet_BaseballClassification
{
    class Program
    {
        private static string _appPath => Path.GetDirectoryName(Environment.GetCommandLineArgs()[0]);
        private static string _trainDataPath => Path.Combine(_appPath, "..", "..", "..", "Data", "BaseballHOFTrainingv2.csv");
        private static string _validationDataPath => Path.Combine(_appPath, "..", "..", "..", "Data", "BaseballHOFValidationv2.csv");

        // Thread-safe ML Context
        private static MLContext _mlContext;

        private static string _labelColunmn = "OnHallOfFameBallot";

        // Configuration Arrays

        // List of feature columns used for training
        // Useage: Comment out (or uncomment) feature names in order to explicitly select features for model training
        private static string[] featureColumns = new string[] {
            "YearsPlayed", "AB", "R", "H", "Doubles", "Triples", "HR", "RBI", "SB",
            "BattingAverage", "SluggingPct", "AllStarAppearances", "MVPs", "TripleCrowns", "GoldGloves",
            "MajorLeaguePlayerOfTheYearAwards", "TB" };
        
        // List of supervised learning labels
        // Useage: At least one must be left
        private static string[] labelColumns = new string[] { "OnHallOfFameBallot", "InductedToHallOfFame" };

        // List of algorithms that support probability output
        // Useage: Comment out (or uncomment) algorithm names to report model explainability
        private static string[] algorithmsForModelExplainability = new string[] {
                "FieldAwareFactorization",
                "GeneralizedAdditiveModels", "LogisticRegression",
                "FastTree", "LightGbm",
                "StochasticGradientDescentCalibrated"
        };

        static void Main(string[] args)
        {
            // Start stopwatch to time model job
            Stopwatch sw = new Stopwatch();
            sw.Start();

            Console.Title = "Baseball Predictions - Training Model Job";
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("Starting Baseball Predictions - Training Model Job");
            Console.WriteLine("Using ML.NET - Version 1.4");
            Console.WriteLine();
            Console.ResetColor();
            Console.WriteLine("This job will build a series of models that will predict both:");
            Console.WriteLine("1) Whether a baseball batter would make it on the HOF Ballot (OnHallOfFameBallot)");
            Console.WriteLine("2) Whether a baseball batter would be inducted to the HOF (InductedToHallOfFame).");
            Console.WriteLine("Based on an MLB batter's summarized career batting statistics.\n");
            Console.WriteLine("Note: The goal is to build a 'good enough' set of models & showcase the ML.NET framework.");
            Console.WriteLine("Note: For better models advanced historical scaling and features should be performed.");
            Console.WriteLine();

            #region Step 1) ML.NET Setup & Load Data

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("###############################");
            Console.WriteLine("Step 1: Load Data from files...");
            Console.WriteLine("###############################\n");
            Console.ResetColor();

            // Set the seed explicitly for reproducability (models will be built with consistent results)
            _mlContext = new MLContext(seed: 200);

            // Read the training/validation data from a text file
            var dataTrain = _mlContext.Data.LoadFromTextFile<MLBBaseballBatter>(path: _trainDataPath,
                hasHeader: true, separatorChar: ',', allowQuoting: false);
            var dataValidation = _mlContext.Data.LoadFromTextFile<MLBBaseballBatter>(path: _validationDataPath,
                hasHeader: true, separatorChar: ',', allowQuoting: false);

            // Retrieve Data Schema
            var dataSchema = dataTrain.Schema;

            #if DEBUG
            // Debug Only: Preview the training/validation data
            var dataTrainPreview = dataTrain.Preview();
            var dataValidationPreview = dataValidation.Preview();
            #endif

            // Cache the loaded data
            var cachedTrainData = _mlContext.Data.Cache(dataTrain);
            var cachedValidationData = _mlContext.Data.Cache(dataValidation);

            #endregion

            #region Step 2) Build Multiple Machine Learning Models

            // Notes:
            // Model training is for demo purposes and uses the default hyperparameters.
            // Default parameters were used in optimizing for large data sets.
            // It is best practice to always provide hyperparameters explicitly in order to have historical reproducability
            // as the ML.NET API evolves.

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("###############################");
            Console.WriteLine("Step 2: Train Models...");
            Console.WriteLine("###############################\n");
            Console.ResetColor();

            /* LIGHTGBM MODELS */
            Console.WriteLine("Training...LightGbm Models.");

            _labelColunmn = "OnHallOfFameBallot";
            // Build simple data pipeline
            var learningPipelineLightGbmOnHallOfFameBallot =
                Utilities.GetBaseLinePipeline(_mlContext, featureColumns).Append(
                _mlContext.BinaryClassification.Trainers.LightGbm(labelColumnName: _labelColunmn)
                );
            // Fit (build a Machine Learning Model)
            var modelLightGbmOnHallOfFameBallot = learningPipelineLightGbmOnHallOfFameBallot.Fit(cachedTrainData);
            // Save the model to storage
            Utilities.SaveModel(_appPath, _mlContext, dataSchema, "LightGbm", _labelColunmn, modelLightGbmOnHallOfFameBallot);
            Utilities.SaveOnnxModel(_appPath, "LightGbm", _labelColunmn, modelLightGbmOnHallOfFameBallot, _mlContext, cachedTrainData);

            _labelColunmn = "InductedToHallOfFame";
            // Build simple data pipeline
            var learningPipelineLightGbmInductedToHallOfFame =
                Utilities.GetBaseLinePipeline(_mlContext, featureColumns).Append(
                _mlContext.BinaryClassification.Trainers.LightGbm(labelColumnName: _labelColunmn)
                );
            // Fit (build a Machine Learning Model)
            var modelLightGbmInductedToHallOfFame = learningPipelineLightGbmInductedToHallOfFame.Fit(cachedTrainData);
            // Save the model to storage
            Utilities.SaveModel(_appPath, _mlContext, dataSchema, "LightGbm", _labelColunmn, modelLightGbmInductedToHallOfFame);
            Utilities.SaveOnnxModel(_appPath, "LightGbm", _labelColunmn, modelLightGbmInductedToHallOfFame, _mlContext, cachedTrainData);


            /* LOGISTIC REGRESSION MODELS */
            Console.WriteLine("Training...Logistic Regression Models.");

            _labelColunmn = "OnHallOfFameBallot";
            // Build simple data pipeline
            var learningPipelineLogisticRegressionOnHallOfFameBallot =
                Utilities.GetBaseLinePipeline(_mlContext, featureColumns).Append(
                _mlContext.BinaryClassification.Trainers.LbfgsLogisticRegression(labelColumnName: _labelColunmn)
                );
            // Fit (build a Machine Learning Model)
            var modelLogisticRegressionOnHallOfFameBallot = learningPipelineLogisticRegressionOnHallOfFameBallot.Fit(cachedTrainData);

            // Save the model to storage
            Utilities.SaveModel(_appPath, _mlContext, dataSchema, "LogisticRegression", _labelColunmn, modelLogisticRegressionOnHallOfFameBallot);
            Utilities.SaveOnnxModel(_appPath, "LogisticRegression", _labelColunmn, modelLogisticRegressionOnHallOfFameBallot, _mlContext, cachedTrainData);

            _labelColunmn = "InductedToHallOfFame";
            // Build simple data pipeline
            var learningPipelineLogisticRegressionInductedToHallOfFame =
                Utilities.GetBaseLinePipeline(_mlContext, featureColumns).Append(
                _mlContext.BinaryClassification.Trainers.LbfgsLogisticRegression(labelColumnName: _labelColunmn)
                );
            // Fit (build a Machine Learning Model)
            var modelLogisticRegressionInductedToHallOfFame = learningPipelineLogisticRegressionInductedToHallOfFame.Fit(cachedTrainData);
            // Save the model to storage
            Utilities.SaveModel(_appPath, _mlContext, dataSchema, "LogisticRegression", _labelColunmn, modelLogisticRegressionInductedToHallOfFame);
            Utilities.SaveOnnxModel(_appPath, "LogisticRegression", _labelColunmn, modelLogisticRegressionInductedToHallOfFame, _mlContext, cachedTrainData);


            /* AVERAGED PERCEPTRON MODELS */
            Console.WriteLine("Training...Averaged Perceptron Models.");

            _labelColunmn = "OnHallOfFameBallot";
            // Build simple data pipeline
            var learningPipelineAveragedPerceptronOnHallOfFameBallot =
                Utilities.GetBaseLinePipeline(_mlContext, featureColumns).Append(
                _mlContext.BinaryClassification.Trainers.AveragedPerceptron(labelColumnName: _labelColunmn, numberOfIterations: 10)
                );
            // Fit (build a Machine Learning Model)
            var modelAveragedPerceptronOnHallOfFameBallot = learningPipelineAveragedPerceptronOnHallOfFameBallot.Fit(cachedTrainData);
            // Save the model to storage
            Utilities.SaveModel(_appPath, _mlContext, dataSchema, "AveragedPerceptron", _labelColunmn, modelAveragedPerceptronOnHallOfFameBallot);
            Utilities.SaveOnnxModel(_appPath, "AveragedPerceptron", _labelColunmn, modelAveragedPerceptronOnHallOfFameBallot, _mlContext, cachedTrainData);

            _labelColunmn = "InductedToHallOfFame";
            // Build simple data pipeline
            var learningPipelineAveragedPerceptronInductedToHallOfFame =
                Utilities.GetBaseLinePipeline(_mlContext, featureColumns).Append(
                _mlContext.BinaryClassification.Trainers.AveragedPerceptron(labelColumnName: _labelColunmn)
                );
            // Fit (build a Machine Learning Model)
            var modelAveragedPerceptronInductedToHallOfFame = learningPipelineAveragedPerceptronInductedToHallOfFame.Fit(cachedTrainData);
            // Save the model to storage
            Utilities.SaveModel(_appPath, _mlContext, dataSchema, "AveragedPerceptron", _labelColunmn, modelAveragedPerceptronInductedToHallOfFame);
            Utilities.SaveOnnxModel(_appPath, "AveragedPerceptron", _labelColunmn, modelAveragedPerceptronInductedToHallOfFame, _mlContext, cachedTrainData);


            /* FAST FOREST MODELS */
            Console.WriteLine("Training...Fast Forest Models.");

            _labelColunmn = "OnHallOfFameBallot";
            // Build simple data pipeline
            var learningPipelineFastForestOnHallOfFameBallot =
                Utilities.GetBaseLinePipeline(_mlContext, featureColumns).Append(
                _mlContext.BinaryClassification.Trainers.FastForest(labelColumnName: _labelColunmn)
                );
            // Fit (build a Machine Learning Model)
            var modelFastForestOnHallOfFameBallot = learningPipelineFastForestOnHallOfFameBallot.Fit(cachedTrainData);
            // Save the model to storage
            Utilities.SaveModel(_appPath, _mlContext, dataSchema, "FastForest", _labelColunmn, modelFastForestOnHallOfFameBallot);
            Utilities.SaveOnnxModel(_appPath, "FastForest", _labelColunmn, modelFastForestOnHallOfFameBallot, _mlContext, cachedTrainData);

            _labelColunmn = "InductedToHallOfFame";
            // Build simple data pipeline
            var learningPipelineFastForestInductedToHallOfFame =
                Utilities.GetBaseLinePipeline(_mlContext, featureColumns).Append(
                _mlContext.BinaryClassification.Trainers.FastForest(labelColumnName: _labelColunmn)
                );
            // Fit (build a Machine Learning Model)
            var modelFastForestInductedToHallOfFame = learningPipelineFastForestInductedToHallOfFame.Fit(cachedTrainData);
            // Save the model to storage
            Utilities.SaveModel(_appPath, _mlContext, dataSchema, "FastForest", _labelColunmn, modelFastForestInductedToHallOfFame);
            Utilities.SaveOnnxModel(_appPath, "FastForest", _labelColunmn, modelFastForestInductedToHallOfFame, _mlContext, cachedTrainData);


            /* FAST TREE MODELS */
            Console.WriteLine("Training...Fast Tree Models.");

            _labelColunmn = "OnHallOfFameBallot";
            // Build simple data pipeline
            var learningPipelineFastTreeOnHallOfFameBallot =
                Utilities.GetBaseLinePipeline(_mlContext, featureColumns).Append(
                _mlContext.BinaryClassification.Trainers.FastTree(labelColumnName: _labelColunmn, learningRate: 0.01, numberOfTrees: 500)
                );
            // Fit (build a Machine Learning Model)
            var modelFastTreeOnHallOfFameBallot = learningPipelineFastTreeOnHallOfFameBallot.Fit(cachedTrainData);
            // Save the model to storage
            Utilities.SaveModel(_appPath, _mlContext, dataSchema, "FastTree", _labelColunmn, modelFastTreeOnHallOfFameBallot);
            Utilities.SaveOnnxModel(_appPath, "FastTree", _labelColunmn, modelFastTreeOnHallOfFameBallot, _mlContext, cachedTrainData);

            _labelColunmn = "InductedToHallOfFame";
            // Build simple data pipeline
            var learningPipelineFastTreeInductedToHallOfFame =
                Utilities.GetBaseLinePipeline(_mlContext, featureColumns).Append(
                _mlContext.BinaryClassification.Trainers.FastTree(labelColumnName: _labelColunmn, learningRate: 0.01, numberOfTrees: 500)
                );
            // Fit (build a Machine Learning Model)
            var modelFastTreeInductedToHallOfFame = learningPipelineFastTreeInductedToHallOfFame.Fit(cachedTrainData);
            // Save the model to storage
            Utilities.SaveModel(_appPath, _mlContext, dataSchema, "FastTree", _labelColunmn, modelFastTreeInductedToHallOfFame);
            Utilities.SaveOnnxModel(_appPath, "FastTree", _labelColunmn, modelFastTreeInductedToHallOfFame, _mlContext, cachedTrainData);


            /* FIELD AWARE FACTORIZATION MODELS */
            Console.WriteLine("Training...Field Aware Factorization Models.");
            _labelColunmn = "OnHallOfFameBallot";
            // Build simple data pipeline
            var learningPipelineFieldAwareFactorizationOnHallOfFameBallot =
                Utilities.GetBaseLinePipeline(_mlContext, featureColumns).Append(
                _mlContext.BinaryClassification.Trainers.FieldAwareFactorizationMachine(featureColumnNames: new[] { "Features" }, labelColumnName: _labelColunmn)
                );
            // Fit (build a Machine Learning Model)
            var modelFieldAwareFactorizationOnHallOfFameBallot = learningPipelineFieldAwareFactorizationOnHallOfFameBallot.Fit(cachedTrainData);
            // Save the model to storage
            Utilities.SaveModel(_appPath, _mlContext, dataSchema, "FieldAwareFactorization", _labelColunmn, modelFieldAwareFactorizationOnHallOfFameBallot);
            Utilities.SaveOnnxModel(_appPath, "FieldAwareFactorization", _labelColunmn, modelFieldAwareFactorizationOnHallOfFameBallot, _mlContext, cachedTrainData);

            _labelColunmn = "InductedToHallOfFame";
            // Build simple data pipeline
            var learningPipelineFieldAwareFactorizationInductedToHallOfFame =
                Utilities.GetBaseLinePipeline(_mlContext, featureColumns).Append(
                _mlContext.BinaryClassification.Trainers.FieldAwareFactorizationMachine(featureColumnNames: new[] { "Features" }, labelColumnName: _labelColunmn)
                );
            // Fit (build a Machine Learning Model)
            var modelFieldAwareFactorizationInductedToHallOfFame = learningPipelineFieldAwareFactorizationInductedToHallOfFame.Fit(cachedTrainData);
            // Save the model to storage
            Utilities.SaveModel(_appPath, _mlContext, dataSchema, "FieldAwareFactorization", _labelColunmn, modelFieldAwareFactorizationInductedToHallOfFame);
            Utilities.SaveOnnxModel(_appPath, "FieldAwareFactorization", _labelColunmn, modelFieldAwareFactorizationInductedToHallOfFame, _mlContext, cachedTrainData);


            /* STOCHASTIC GRADIENT DESCENT - CALIBRATED MODELS */
            Console.WriteLine("Training...Stochastic Gradient Descent - Calibrated Models.");

            _labelColunmn = "OnHallOfFameBallot";
            // Build simple data pipeline
            var learningPipelineStochasticGradientDescentCalibratedOnHallOfFameBallot =
                Utilities.GetBaseLinePipeline(_mlContext, featureColumns).Append(
                _mlContext.BinaryClassification.Trainers.SgdCalibrated(labelColumnName: _labelColunmn)
                );
            // Fit (build a Machine Learning Model)
            var modelStochasticGradientDescentCalibratedOnHallOfFameBallot = learningPipelineStochasticGradientDescentCalibratedOnHallOfFameBallot.Fit(cachedTrainData);
            // Save the model to storage
            Utilities.SaveModel(_appPath, _mlContext, dataSchema, "StochasticGradientDescentCalibrated", _labelColunmn, modelStochasticGradientDescentCalibratedOnHallOfFameBallot);
            Utilities.SaveOnnxModel(_appPath, "StochasticGradientDescentCalibrated", _labelColunmn, modelStochasticGradientDescentCalibratedOnHallOfFameBallot, _mlContext, cachedTrainData);

            _labelColunmn = "InductedToHallOfFame";
            // Build simple data pipeline
            var learningPipelineStochasticGradientDescentCalibratedInductedToHallOfFame =
                Utilities.GetBaseLinePipeline(_mlContext, featureColumns).Append(
                _mlContext.BinaryClassification.Trainers.SgdCalibrated(labelColumnName: _labelColunmn)
                );
            // Fit (build a Machine Learning Model)
            var modelStochasticGradientDescentCalibratedInductedToHallOfFame = learningPipelineStochasticGradientDescentCalibratedInductedToHallOfFame.Fit(cachedTrainData);
            // Save the model to storage
            Utilities.SaveModel(_appPath, _mlContext, dataSchema, "StochasticGradientDescentCalibrated", _labelColunmn, modelStochasticGradientDescentCalibratedInductedToHallOfFame);
            Utilities.SaveOnnxModel(_appPath, "StochasticGradientDescentCalibrated", _labelColunmn, modelStochasticGradientDescentCalibratedInductedToHallOfFame, _mlContext, cachedTrainData);

            /* STOCHASTIC GRADIENT DESCENT - NON CALIBRATED MODELS */
            Console.WriteLine("Training...Stochastic Gradient Descent - NonCalibrated Models.");

            _labelColunmn = "OnHallOfFameBallot";
            // Build simple data pipeline
            var learningPipelineStochasticGradientDescentNonCalibratedOnHallOfFameBallot =
                Utilities.GetBaseLinePipeline(_mlContext, featureColumns).Append(
                _mlContext.BinaryClassification.Trainers.SgdNonCalibrated(labelColumnName: _labelColunmn)
                );
            // Fit (build a Machine Learning Model)
            var modelStochasticGradientDescentNonCalibratedOnHallOfFameBallot = learningPipelineStochasticGradientDescentNonCalibratedOnHallOfFameBallot.Fit(cachedTrainData);
            // Save the model to storage
            Utilities.SaveModel(_appPath, _mlContext, dataSchema, "StochasticGradientDescentNonCalibrated", _labelColunmn, modelStochasticGradientDescentNonCalibratedOnHallOfFameBallot);
            Utilities.SaveOnnxModel(_appPath, "StochasticGradientDescentNonCalibrated", _labelColunmn, modelStochasticGradientDescentNonCalibratedOnHallOfFameBallot, _mlContext, cachedTrainData);

            _labelColunmn = "InductedToHallOfFame";
            // Build simple data pipeline
            var learningPipelineStochasticGradientDescentNonCalibratedInductedToHallOfFame =
                Utilities.GetBaseLinePipeline(_mlContext, featureColumns).Append(
                _mlContext.BinaryClassification.Trainers.SgdNonCalibrated(labelColumnName: _labelColunmn)
                );
            // Fit (build a Machine Learning Model)
            var modelStochasticGradientDescentNonCalibratedInductedToHallOfFame = learningPipelineStochasticGradientDescentNonCalibratedInductedToHallOfFame.Fit(cachedTrainData);
            // Save the model to storage
            Utilities.SaveModel(_appPath, _mlContext, dataSchema, "StochasticGradientDescentNonCalibrated", _labelColunmn, modelStochasticGradientDescentNonCalibratedInductedToHallOfFame);
            Utilities.SaveOnnxModel(_appPath, "StochasticGradientDescentNonCalibrated", _labelColunmn, modelStochasticGradientDescentNonCalibratedInductedToHallOfFame, _mlContext, cachedTrainData);


            // TODO: Fix for PFI
            //var transformedData = modelStochasticGradientDescentInductedToHallOfFame.Transform(cachedTrainData);
            //var permutationFeatureImportance =
            //_mlContext.BinaryClassification.PermutationFeatureImportance(modelStochasticGradientDescentInductedToHallOfFame.LastTransformer, 
            //data: transformedData, labelColumnName: _labelColunmn);
            //Microsoft.ML.Data.TransformerChain<Microsoft.ML.Data.BinaryPredictionTransformer<Microsoft.ML.Calibrators.CalibratedModelParametersBase<Microsoft.ML.Trainers.LinearBinaryModelParameters, Microsoft.ML.Calibrators.PlattCalibrator>>> test;
            //test = modelStochasticGradientDescentInductedToHallOfFame;
            //test = null;
            //var loadedModelTest = Utilities.LoadModel(_mlContext,
            //    Utilities.GetModelPath(_appPath, algorithmName: algorithmsForModelExplainability[0], isOnnx: false, label: _labelColunmn));


            /* GENERALIZED ADDITIVE MODELS */
            Console.WriteLine("Training...Generalized Additive Models.");

            _labelColunmn = "OnHallOfFameBallot";
            // Build simple data pipeline
            var learningPipelineGeneralizedAdditiveModelsOnHallOfFameBallot =
                Utilities.GetBaseLinePipeline(_mlContext, featureColumns).Append(
                _mlContext.BinaryClassification.Trainers.Gam(labelColumnName: _labelColunmn)
                );
            // Fit (build a Machine Learning Model)
            var modelGeneralizedAdditiveModelsOnHallOfFameBallot = learningPipelineGeneralizedAdditiveModelsOnHallOfFameBallot.Fit(cachedTrainData);
            // Save the model to storage
            Utilities.SaveModel(_appPath, _mlContext, dataSchema, "GeneralizedAdditiveModels", _labelColunmn, modelGeneralizedAdditiveModelsOnHallOfFameBallot);
            Utilities.SaveOnnxModel(_appPath, "GeneralizedAdditiveModels", _labelColunmn, modelGeneralizedAdditiveModelsOnHallOfFameBallot, _mlContext, cachedTrainData);

            _labelColunmn = "InductedToHallOfFame";
            // Build simple data pipeline
            var learningPipelineGeneralizedAdditiveModelsInductedToHallOfFame =
                Utilities.GetBaseLinePipeline(_mlContext, featureColumns).Append(
                _mlContext.BinaryClassification.Trainers.Gam(labelColumnName: _labelColunmn)
                );
            // Fit (build a Machine Learning Model)
            var modelGeneralizedAdditiveModelsInductedToHallOfFame = learningPipelineGeneralizedAdditiveModelsInductedToHallOfFame.Fit(cachedTrainData);
            // Save the model to storage
            Utilities.SaveModel(_appPath, _mlContext, dataSchema, "GeneralizedAdditiveModels", _labelColunmn, modelGeneralizedAdditiveModelsInductedToHallOfFame);
            Utilities.SaveOnnxModel(_appPath, "GeneralizedAdditiveModels", _labelColunmn, modelGeneralizedAdditiveModelsInductedToHallOfFame, _mlContext, cachedTrainData);


            /* LINEAR SUPPORT VECTOR MODELS */
            Console.WriteLine("Training...Linear Support Vector Models.");

            _labelColunmn = "OnHallOfFameBallot";
            // Build simple data pipeline
            var learningPipelineLinearSupportVectorMachinesOnHallOfFameBallot =
                Utilities.GetBaseLinePipeline(_mlContext, featureColumns).Append(
                _mlContext.BinaryClassification.Trainers.LinearSvm(labelColumnName: _labelColunmn, numberOfIterations: 10)
                );
            // Fit (build a Machine Learning Model)
            var modelLinearSupportVectorMachinesOnHallOfFameBallot = learningPipelineLinearSupportVectorMachinesOnHallOfFameBallot.Fit(cachedTrainData);
            // Save the model to storage
            Utilities.SaveModel(_appPath, _mlContext, dataSchema, "LinearSupportVectorMachines", _labelColunmn, modelLinearSupportVectorMachinesOnHallOfFameBallot);
            Utilities.SaveOnnxModel(_appPath, "LinearSupportVectorMachines", _labelColunmn, modelLinearSupportVectorMachinesOnHallOfFameBallot, _mlContext, cachedTrainData);

            _labelColunmn = "InductedToHallOfFame";
            // Build simple data pipeline
            var learningPipelineLinearSupportVectorMachinesInductedToHallOfFame =
                Utilities.GetBaseLinePipeline(_mlContext, featureColumns).Append(
                _mlContext.BinaryClassification.Trainers.LinearSvm(labelColumnName: _labelColunmn)
                );
            // Fit (build a Machine Learning Model)
            var modelLinearSupportVectorMachinesInductedToHallOfFame = learningPipelineLinearSupportVectorMachinesInductedToHallOfFame.Fit(cachedTrainData);
            // Save the model to storage
            Utilities.SaveModel(_appPath, _mlContext, dataSchema, "LinearSupportVectorMachines", _labelColunmn, modelLinearSupportVectorMachinesInductedToHallOfFame);
            Utilities.SaveOnnxModel(_appPath, "LinearSupportVectorMachines", _labelColunmn, modelLinearSupportVectorMachinesInductedToHallOfFame, _mlContext, cachedTrainData);


            //var test = _mlContext.BinaryClassification.CrossValidate(cachedTrainData, learningPipelineLightGbmInductedToHallOfFame, 100,
            //    labelColumn: _labelColunmn, stratificationColumn: _labelColunmn);

            Console.WriteLine(string.Empty);

            #endregion

            #region Step 3) Report Performance Metrics

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("###############################");
            Console.WriteLine("Step 3: Report Metrics...");
            Console.WriteLine("###############################\n");
            Console.ResetColor();

            for (int i = 0; i < algorithmsForModelExplainability.Length; i++)
            {
                for (int j = 0; j < labelColumns.Length; j++)
                {
                    var binaryClassificationMetrics = Utilities.GetBinaryClassificationModelMetrics(_appPath, _mlContext, labelColumns[j], algorithmsForModelExplainability[i], cachedValidationData);

                    Console.WriteLine("Evaluation Metrics for " + algorithmsForModelExplainability[i] + " | " + labelColumns[j]);
                    Console.WriteLine("**************************");
                    Console.WriteLine("F1 Score:                 " + Math.Round(binaryClassificationMetrics.F1Score, 4).ToString());
                    Console.WriteLine("AUC - ROC Score:          " + Math.Round(binaryClassificationMetrics.AreaUnderRocCurve, 4).ToString());
                    Console.WriteLine("AUC - Prec/Recall Score:  " + Math.Round(binaryClassificationMetrics.AreaUnderPrecisionRecallCurve, 4).ToString());
                    Console.WriteLine("Precision:                " + Math.Round(binaryClassificationMetrics.PositivePrecision, 4).ToString());
                    Console.WriteLine("Recall:                   " + Math.Round(binaryClassificationMetrics.PositiveRecall, 4).ToString());
                    Console.WriteLine("Accuracy:                 " + Math.Round(binaryClassificationMetrics.Accuracy, 4).ToString());
                    Console.WriteLine("LogLoss:                  " + Math.Round(binaryClassificationMetrics.LogLoss, 4).ToString());
                    Console.WriteLine("**************************");

                    var loadedModel = Utilities.LoadModel(_mlContext, Utilities.GetModelPath(_appPath, algorithmName: algorithmsForModelExplainability[i], isOnnx: false, label: labelColumns[j]));
                    var transformedModelData = loadedModel.Transform(cachedValidationData);

                    ITransformer lModel = loadedModel;
                    //_mlContext.BinaryClassification.PermutationFeatureImportance(lModel, transformedModelData);

                    var lastTran = loadedModel.LastTransformer;
                    //var enumerator = lastTran.GetEnumerator();

                    // TODO: Check for PFI support
                    ISingleFeaturePredictionTransformer<ModelParametersBase<float>> transfomerForPfi = null; // lastTran;
                    //   (ISingleFeaturePredictionTransformer<ModelParametersBase<float>>) lastTran;


                    //_mlContext.BinaryClassification.PermutationFeatureImportance(modelStochasticGradientDescentInductedToHallOfFame.LastTransformer, data: transformedData, labelColumnName: _labelColunmn);
                    //_mlContext.BinaryClassification.PermutationFeatureImportance(lastTran, data: cachedTrainData, labelColumnName: labelColumns[j]);

                    //if (transfomerForPfi != null)
                    //{
                    //    _mlContext.BinaryClassification.PermutationFeatureImportance(transfomerForPfi, transformedModelData);
                    //}


                    //ISingleFeaturePredictionTransformer<IPredictorProducing<float>> transfomerForPfi = null;
                    //while (enumerator.MoveNext())
                    //{
                    //    if (enumerator.Current is IPredictionTransformer<ModelParametersBase<float>>)
                    //    {
                    //        transfomerForPfi = enumerator.Current as ISingleFeaturePredictionTransformer<ModelParametersBase<float>>;
                    //    }
                    //}

                    if (transfomerForPfi != null)
                    {
                        // Console.WriteLine("!!!!!!!!HEELLO");
                        //_mlContext.BinaryClassification.PermutationFeatureImportance(loadedModel.LastTransformer, null);
                        //// TODO: FIX
                        //// Retrieve Top Features based on Permutation Feature Importance
                        //var permutationMetrics = _mlContext.BinaryClassification.PermutationFeatureImportance(model: loadedModel.LastTransformer, data: transformedModelData,
                        // label: labelColumns[j], features: "Features", useFeatureWeightFilter: false, permutationCount: 10);

                        //// Build a list of feature importance metrics
                        //List<FeatureImportanceValue> featureImportanceValues = new List<FeatureImportanceValue>();
                        //for (int k = 0; k < permutationMetrics.Length; k++)
                        //{
                        //    featureImportanceValues.Add(
                        //            new FeatureImportanceValue
                        //            {
                        //                FeatureName = featureColumns[k],
                        //                PerformanceMetricName = "F1Score.Mean",
                        //                PerformanceMetricValue = permutationMetrics[k].F1Score.Mean
                        //            }
                        //        );
                        //}

                        //// Filter out NaN values and order by lowest values
                        //// Note: Should be done with absolute and check for positive values for features
                        //var orderedFeatures = featureImportanceValues.Where(a => !Double.IsNaN(a.PerformanceMetricValue)).OrderBy(a => a.PerformanceMetricValue).ToList();
                        //var numberOfFeaturesToReport = 4;

                        //Console.WriteLine("Most important features (" + numberOfFeaturesToReport + ")");
                        //Console.WriteLine("******************");

                        //for (int l = 0; l < numberOfFeaturesToReport; l++)
                        //{
                        //    if (l + 1 <= featureImportanceValues.Count && l < orderedFeatures.Count)
                        //    {
                        //        Console.WriteLine(orderedFeatures[l].FeatureName + ": " + Math.Round(orderedFeatures[l].PerformanceMetricValue, 4).ToString());
                        //    }
                        //}
                    }
                    else
                    {
                        // TODO: FIX in post v1.0+

                        //Console.WriteLine("Most important features ()");
                        //Console.WriteLine("******************");
                        //Console.WriteLine("Model's algorithm does not support explainability.");
                    }

                    Console.WriteLine("**************************");
                    Console.WriteLine();
                }
            }



            #endregion

            #region Step 4) New Predictions - Using Ficticious Player Data

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("###############################");
            Console.WriteLine("Step 4: New Predictions...");
            Console.WriteLine("###############################\n");
            Console.ResetColor();

            // Set algorithm type to use for predictions
            // Retrieve model path
            // TODO: Hardcoded add perscriptive rules engine
            var algorithmTypeName = "GeneralizedAdditiveModels";
            var loadedModelOnHallOfFameBallot = Utilities.LoadModel(_mlContext, (Utilities.GetModelPath(_appPath, algorithmTypeName, false, "OnHallOfFameBallot")));
            var loadedModelInductedToHallOfFame = Utilities.LoadModel(_mlContext, (Utilities.GetModelPath(_appPath, algorithmTypeName, false, "InductedToHallOfFame")));

            // Create prediction engine
            var predEngineOnHallOfFameBallot = _mlContext.Model.CreatePredictionEngine<MLBBaseballBatter, MLBHOFPrediction>(loadedModelOnHallOfFameBallot);
            var predEngineInductedToHallOfFame = _mlContext.Model.CreatePredictionEngine<MLBBaseballBatter, MLBHOFPrediction>(loadedModelInductedToHallOfFame);

            // Create statistics for bad, average & great player
            var badMLBBatter = new MLBBaseballBatter
            {
                FullPlayerName = "Bad Player",
                ID = 100f,
                InductedToHallOfFame = false,
                LastYearPlayed = 0f,
                OnHallOfFameBallot = false,
                YearsPlayed = 2f,
                AB = 100f,
                R = 10f,
                H = 30f,
                Doubles = 1f,
                Triples = 1f,
                HR = 1f,
                RBI = 10f,
                SB = 10f,
                BattingAverage = 0.3f,
                SluggingPct = 0.15f,
                AllStarAppearances = 1f,
                MVPs = 0f,
                TripleCrowns = 0f,
                GoldGloves = 0f,
                MajorLeaguePlayerOfTheYearAwards = 0f,
                TB = 200f
            };
            var averageMLBBatter = new MLBBaseballBatter
            {
                FullPlayerName = "Average Player",
                ID = 100f,
                InductedToHallOfFame = false,
                LastYearPlayed = 0f,
                OnHallOfFameBallot = false,
                YearsPlayed = 2f,
                AB = 8393f,
                R = 1162f,
                H = 2340f,
                Doubles = 410f,
                Triples = 8f,
                HR = 439f,
                RBI = 1412f,
                SB = 9f,
                BattingAverage = 0.279f,
                SluggingPct = 0.486f,
                AllStarAppearances = 6f,
                MVPs = 0f,
                TripleCrowns = 0f,
                GoldGloves = 0f,
                MajorLeaguePlayerOfTheYearAwards = 0f,
                TB = 4083f
            };
            var greatMLBBatter = new MLBBaseballBatter
            {
                FullPlayerName = "Great Player",
                ID = 100f,
                InductedToHallOfFame = false,
                LastYearPlayed = 0f,
                OnHallOfFameBallot = false,
                YearsPlayed = 20f,
                AB = 10000f,
                R = 1900f,
                H = 3500f,
                Doubles = 500f,
                Triples = 150f,
                HR = 600f,
                RBI = 1800f,
                SB = 400f,
                BattingAverage = 0.350f,
                SluggingPct = 0.65f,
                AllStarAppearances = 14f,
                MVPs = 2f,
                TripleCrowns = 1f,
                GoldGloves = 4f,
                MajorLeaguePlayerOfTheYearAwards = 2f,
                TB = 7000f
            };

            var batters = new List<MLBBaseballBatter> { badMLBBatter, averageMLBBatter, greatMLBBatter };
            // Convert the list to an IDataView
            var newPredictionsData = _mlContext.Data.LoadFromEnumerable(batters);

            // Make the predictions for both OnHallOfFameBallot & InductedToHallOfFame
            var predBadOnHallOfFameBallot = predEngineOnHallOfFameBallot.Predict(badMLBBatter);
            var predBadInductedToHallOfFame = predEngineInductedToHallOfFame.Predict(badMLBBatter);
            var predAverageOnHallOfFameBallot = predEngineOnHallOfFameBallot.Predict(averageMLBBatter);
            var predAverageInductedToHallOfFame = predEngineInductedToHallOfFame.Predict(averageMLBBatter);
            var predGreatOnHallOfFameBallot = predEngineOnHallOfFameBallot.Predict(greatMLBBatter);
            var predGreatInductedToHallOfFame = predEngineInductedToHallOfFame.Predict(greatMLBBatter);

            // Report the results
            Console.WriteLine("Algorithm Used for sample Model Prediction: " + algorithmTypeName);
            Console.WriteLine("\n");
            Console.WriteLine("Bad Baseball Player Prediction");
            Console.WriteLine("------------------------------");
            Console.WriteLine("On HOF Ballot Prediction: " + predBadOnHallOfFameBallot.Prediction.ToString() + " | " + "Probability: " + predBadOnHallOfFameBallot.Probability);
            Console.WriteLine("HOF Inducted Prediction:  " + predBadInductedToHallOfFame.Prediction.ToString() + " | " + "Probability: " + predBadInductedToHallOfFame.Probability);
            Console.WriteLine();
            Console.WriteLine("Average Baseball Player Prediction");
            Console.WriteLine("------------------------------");
            Console.WriteLine("On HOF Ballot Prediction: " + predAverageOnHallOfFameBallot.Prediction.ToString() + " | " + "Probability: " + predAverageOnHallOfFameBallot.Probability);
            Console.WriteLine("HOF Inducted Prediction:  " + predAverageInductedToHallOfFame.Prediction.ToString() + " | " + "Probability: " + predAverageInductedToHallOfFame.Probability);
            Console.WriteLine();
            Console.WriteLine("Great Baseball Player Prediction");
            Console.WriteLine("------------------------------");
            Console.WriteLine("On HOF Ballot Prediction: " + predGreatOnHallOfFameBallot.Prediction.ToString() + " | " + "Probability: " + predGreatOnHallOfFameBallot.Probability);
            Console.WriteLine("HOF Inducted Prediction:  " + predGreatInductedToHallOfFame.Prediction.ToString() + " | " + "Probability: " + predGreatInductedToHallOfFame.Probability);

            #endregion


            // TODO: FINISH

            //var loadedModelPath = GetModelPath("LightGbm", true, "OnHallOfFameBallot");
            //var session = new InferenceSession(loadedModelPath);
            //var inputInfo = session.InputMetadata.First();
            //var outputInfo = session.OutputMetadata.First();

            //VBuffer<float> weights = new VBuffer<float>();
            //modelLogisticRegressionInductedToHallOfFame.LastTransformer.Model.GetFeatureWeights(ref weights);

            //var transformedNewPredictionsData = modelLogisticRegressionInductedToHallOfFame.Transform(newPredictionsData);
            //var explainer = _mlContext.Model.Explainability.FeatureContributionCalculation(modelLogisticRegressionInductedToHallOfFame.LastTransformer.Model);
            //var outputData = explainer.Fit(transformedNewPredictionsData).Transform(transformedNewPredictionsData);

            //var scoringEnumerator = _mlContext.CreateEnumerable<BaseballBatterScoreAndFeatureContribution>(outputData, true).GetEnumerator();

            //int index = 0;
            //Console.WriteLine("Probability\tScore\tBiggestFeature      \t\tValue\tWeight\tContribution");
            //while (scoringEnumerator.MoveNext() && index < 4)
            //{
            //    var row = scoringEnumerator.Current;

            //    // Get the feature index with the biggest contribution
            //    var featureOfInterest = GetMostContributingFeature(row.FeatureContributions);

            //    // And the corresponding information about the feature
            //    var value = row.Features[featureOfInterest];
            //    var contribution = row.FeatureContributions[featureOfInterest];
            //    var name = featureColumns[featureOfInterest];
            //    var weight = weights.GetValues()[featureOfInterest];

            //    Console.WriteLine("{0:0.00}\t{1:0.00}\t\t{2}\t{3:0.00}\t{4:0.00}\t{5:0.00}",
            //        row.Probability,
            //        row.Score,
            //        name,
            //        value,
            //        weight,
            //        contribution
            //        );

            //    index++;
            //}

            // End of job, report time
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(string.Format("Finished Baseball Predictions - Training Model Job in: {0} seconds", Math.Round(sw.Elapsed.TotalSeconds, 2)));
            Console.ReadLine();
        }
    }
}
