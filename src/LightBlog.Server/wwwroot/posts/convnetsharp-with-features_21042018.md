# Using ConvNetSharp With Feature Based Data #

[ConvNetSharp](https://github.com/cbovar/ConvNetSharp) which is descended from [ConvNetJs](https://github.com/karpathy/convnetjs) is a library which enables you to use Neural Networks in .NET without the need to call out to other languages or services.

ConvNetSharp also has GPU support which makes it a good option for training networks.

Since much of the interest (and as a result the guides) around Neural Networks focuses on their utility in image analysis, it's slightly unclear how to apply these libraries to numeric and categorical features you may be used to using for SVMs or other machine learning methods.

The aim of this blog post is to note how to acheive this.

Let's take the example of some data observed in a scientific experiment. Perhaps we are trying to predict which snails make good racing snails.

Our data set looks like this:

    Age   Stalk Height    Shell Diameter    Shell Color   Good Snail?
    1     0.52            7.6               Light Brown   No
    1.2   0.74            6.75              Brown         Yes
    1.16  0.73            7.01              Grey          Yes
    etc...

ConvNetSharp uses the concept of ```Volume```s to deal with input and classification data. A Volume is a 4 dimensional shape containing data.

The 4 dimensions are:

1. Width
2. Height
3. Depth
4. Number of rows/observations

Width and height only make sense when dealing with image data so we set them to 1. For our data the depth is equal to our number of features. Because we need to map our categorical features to numbers we end up with the following features:

+ Age
+ Stalk Height
+ Shell Diameter
+ Is Shell Brown
+ Is Shell Light Brown
+ Is Shell Grey

So the depth of our input volume is 6.

Let's create our Net first:

    Net<double> net = new ConvNetSharp.Core.Net<double>();

Now we define the input layer based on the shape of the data we will provide to it:

    // Define the width and height (1) and the depth equal to our number of features
    net.AddLayer(new InputLayer(1, 1, 6));

Then we define some layers:

    net.AddLayer(new FullyConnLayer(32));
    net.AddLayer(new ReluLayer());
    
    // ConvNetSharp supports dropout layers.
    // net.AddLayer(new DropoutLayer(0.3));
    
    net.AddLayer(new FullyConnLayer(32));
    net.AddLayer(new ReluLayer());

    // The last layer before the Softmax for classification must have as many neurons as output features
    net.AddLayer(new FullyConnLayer(2));
    net.AddLayer(new SoftmaxLayer(2));

Our last layer is a SoftMax layer which will predict 2 classes, false and true.

To provide our data to train the network we need to take the following steps:

+ (Optionally) [Resample the data](https://www.kaggle.com/general/10620) if one output is strongly represented in the training data to balance out the classes.
+ Split the data into a training and test set, the trained network will be evaluated against the test set which is not provided to it during training.
+ [Normalize the data](https://en.wikipedia.org/wiki/Feature_scaling). Store the normalization parameters applied to the training set and apply them against the test set.

First we create a trainer for our network, we can either use Stochastic Gradient Descent (SGD) or an [Adam](https://machinelearningmastery.com/adam-optimization-algorithm-for-deep-learning/) trainer.

We will use SGD for this demo:

    var trainer = new ConvNetSharp.Core.Training.Double.SgdTrainer()
    {
      LearningRate = 0.01,
      L2Decay = 0.001,
      BatchSize = 70
    };

Once we have normalzed our training data we must convert it to a volume to feed into the trainer.

If our training set has 70 rows we have input data of the following form - ```double[70][6]``` - where each row is an array containing the numeric value of each of the 6 features.

Our output data is then - ```bool[70]``` - where each entry indicates the result, whether or not the snail is good at racing, for each row.

To convert the input data we need our volume to have width and height of 1, a depth of 6 (the number of features) and a 4th dimension equal to the number of rows in the dataset.

Prior to this we need to flatten our input into a single array of the form - ```double[70 * 6]```. To do this each row should be mapped to a consecutive set of 6 entries in the array. For example:

    var featuresPerRow = input[0].Length;

    var flattenedInput = new double[input.Length * featuresPerRow];
    for (int i = 0; i < input.Length; i++)
    {
        var row = input[i];
        for (int j = 0; j < row.Length; j++)
        {
          flattenedInput[(i * featuresPerRow) + j] = row[j];
        }
    }
This takes each row in the input and maps it to a run of 6 numbers in the flattened array.

Now we can convert this flattened array to a volume:

    var volume = BuilderInstance.Volume.From(flattenedInput, new Shape(1, 1, 6, 70));

Where 70 is whatever the number of rows in the input data was and 6 is the number of features in each row.

The output volume is then a boolean array flattened to 2 numbers per boolean:

    var flattenedOutput = new double[output.Length * 2];
    for (int i = 0; i < output.Length; i++)
    {
      var value = output[i];
      flattenedOutput[2 * i] = value ? 0 : 1;
      flattenedOutput[(2 * i) + 1] = value ? 1 : 0;
    }

    var outputVolume = BuilderInstance.Volume.From(flattenedOutput, 
    new Shape(1, 1, 2, output.Length));

This creates an array where each pair of numbers indicates either false (0, 1) or true(1, 0).

To create our test volume:

    double[][] testSet;
    // Apply whatever normalization technique you are using, e.g. min-max
    double[][] normalizedTestSet = Normalize(testSet, trainingSetParameters);

    // Apply the same flattening process as we did for our training input array
    double[] flattenedTestSet = Flatten(normalizedTestSet);

    var testVolume = BuilderInstance.Volume.From(flattenedTestSet, 
    new Shape(1, 1, 6, normalizedTestSet.Length));

Then we need to run the training process multiple times (epochs) until we start to converge on an accuracy for our test set:

    for (int i = 0; i < maxEpochs; i++)
    {
      trainer.Train(volume, outputVolume);

      // Evaluate the test set accuracy
      var predictions = net.Forward(testVolume);

      var numberWrong = 0;
      for (int j = 0; j < testOutput.Length; j++)
      {
        var predictedFalseValue = predictions.Get(0, 0, 0, j);

        var predictedValue = predictedFalseValue < 0.5 ? true : false;

        if (predictedValue != testOutput[j])
        {
          numberWrong++;
        }
      }

      var percentIncorrect = (numberWrong /(double) testOutput.Length) * 100;
    }

If you've used a dropout layer in the net remember to disable it before evaluating the test set.

This is the most basic way of training the network. You can enhance this by:

+ Splitting the training of each epoch into steps by partitioning your input set into batches of, for example, 20 rows.
+ [Cross validating](https://www.kaggle.com/dansbecker/cross-validation) by switching the data used in the test and training data multiple times.
+ Adjusting or automatically tuning training parameters or network shape/number of layers.

If the output you want to predict is a continuous value rather than a classification you need to train a regression. To do this you swap the last SoftMax layer with a Regression layer. For details on this [see this example](https://github.com/cbovar/ConvNetSharp/blob/master/Examples/Regression1DDemo/Program.cs "Example from GitHub").