using System;
using System.Collections.Generic;
using System.Globalization;             // For NumberStyles
using System.Linq;
using System.Windows.Media;             // In the assembly PresentationCore.  Also requires the assembly WindowsBase.
using System.Windows.Media.Imaging;     // In the assembly PresentationCore.  Also requires the assembly WindowsBase.
using System.Windows.Threading;
using System.Xml;
using System.Text;

namespace WPFLife.Engine
{
    public interface ILifeWindow
    {
        void SetGenerationMessage(int generationNumber);
        void SetRules(bool rulesParam);
        void SetAutoStop(bool autoStopParam);
    }

    public class Engine
    {
        public readonly ILifeWindow window;
        public readonly WriteableBitmap bitmap = null;
        public readonly int canvasWidthInPixels;
        public readonly int canvasHeightInPixels;
        public const int cellSize = 8;
        public readonly int numColumnsOfCells;
        public readonly int numRowsOfCells;
        public readonly int numCellsInUniverse;
        public int[] currentBuffer;
        public int[] previousBuffer;
        public int[] secondPreviousBuffer;
        public readonly int[] memoryBuffer;
        public readonly List<Color> cellColourArray = new List<Color>();
        public readonly int maxCellAge;
        public int globalRunID = 0;
        public const int defaultIntergenerationalDelay = 300; // In milliseconds
        public int intergenerationalDelay = defaultIntergenerationalDelay;
        public const int numNeighbours = 8;
        public int generationNumber = 0;
        public bool autoStop = true;
        public bool rules3_4Life = false;
        public bool rememberAutoStop = true;
        public bool rememberRules3_4Life = false;
        public readonly DispatcherTimer dispatcherTimer = new DispatcherTimer();

        public Engine(ILifeWindow window, int canvasWidthInPixels, int canvasHeightInPixels)
        {
            this.window = window;
            this.canvasWidthInPixels = canvasWidthInPixels;
            this.canvasHeightInPixels = canvasHeightInPixels;
            this.bitmap = BitmapFactory.New(this.canvasWidthInPixels, this.canvasHeightInPixels);

            this.numColumnsOfCells = this.canvasWidthInPixels / cellSize;
            this.numRowsOfCells = this.canvasHeightInPixels / cellSize;
            this.numCellsInUniverse = this.numRowsOfCells * this.numColumnsOfCells;
            this.currentBuffer = new int[this.numCellsInUniverse];
            this.previousBuffer = new int[this.numCellsInUniverse];
            this.secondPreviousBuffer = new int[this.numCellsInUniverse];
            this.memoryBuffer = new int[this.numCellsInUniverse];

            // Dead cells are black; living cells are white.
            cellColourArray.Add(Colors.Black);          // Black: luminosity = 0%
            cellColourArray.Add(Colors.Yellow);         // Yellow: luminosity = 89%
            cellColourArray.Add(Colors.Cyan);           // Cyan: luminosity = 70%
            cellColourArray.Add(Colors.Lime);           // Green: luminosity = 59%
            cellColourArray.Add(Colors.Magenta);        // Magenta: luminosity = 41%
            cellColourArray.Add(Colors.Red);            // Red: luminosity = 30%
            cellColourArray.Add(Colors.White);          // White: luminosity = 100%
            maxCellAge = cellColourArray.Count - 1;

            clearPreviousBuffers();
            clearBuffer(memoryBuffer);

            dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, intergenerationalDelay);

            btnRandom_onClick();
        }

        private void setRules(bool rulesParam)
        {
            rules3_4Life = rulesParam;
            window.SetRules(rulesParam);
        }

        private void setAutoStop(bool autoStopParam)
        {
            autoStop = autoStopParam;
            window.SetAutoStop(autoStopParam);
        }

        private void clearBuffer(int[] buffer)
        {

            for (var i = 0; i < buffer.Length; ++i)
            {
                buffer[i] = 0;
            }
        }

        private void clearPreviousBuffers()
        {
            clearBuffer(previousBuffer);
            clearBuffer(secondPreviousBuffer);
        }

        private bool copyBuffer(int[] sourceBuffer, int[] destinationBuffer)
        {

            if (sourceBuffer.Length != destinationBuffer.Length)
            {
                //alert("copyBuffer(): Error: Buffers differ in length");
                return false;
            }

            for (var i = 0; i < sourceBuffer.Length; ++i)
            {
                destinationBuffer[i] = sourceBuffer[i];
            }

            return true;
        }

        private bool buffersAreEqual(int[] buffer1, int[] buffer2) 
        {

            if (buffer1.Length != buffer2.Length)
            {
                return false;
            }

            for (var i = 0; i < buffer1.Length; ++i)
            {

                if (buffer1[i] != buffer2[i])
                {
                    return false;
                }
            }

            return true;
        }

        private void displayGenerationNumber() 
        {
            window.SetGenerationMessage(generationNumber);
        }

        private void resetGenerationNumber()
        {
            generationNumber = 0;
            displayGenerationNumber();
            clearPreviousBuffers();
            setRules(false);
            setAutoStop(true);
        }

        private void clearCurrentBuffer()
        {
            ++globalRunID;
            clearBuffer(currentBuffer);
        }

        private void displayCurrentBuffer()
        {

            using (bitmap.GetBitmapContext())
            {
                var cellIndex = 0;

                bitmap.Clear(cellColourArray[0]);

                for (var top = 0; top < canvasHeightInPixels; top += cellSize)
                {

                    for (var left = 0; left < canvasWidthInPixels; left += cellSize)
                    {
                        var cellAge = currentBuffer[cellIndex];

                        if (cellAge > 0)
                        {
                            // ThAW 2012/10/12 : The -1 compensates for a bug in the WriteableBitmapEx library version 1.0.3.0.
                            bitmap.FillRectangle(left, top, left + cellSize, top + cellSize - 1, cellColourArray[cellAge]);
                        }

                        ++cellIndex;
                    }
                }
            }
        }

        private void randomizeCurrentBuffer()
        {
            var r = new Random();

            ++globalRunID;

            for (var i = 0; i < currentBuffer.Length; ++i) 
            {
                currentBuffer[i] = (r.Next(3) == 0) ? 1 : 0;
            }
        }

        private void computeAndDisplayNextGeneration(int runID, bool singleStep) 
        {

            if (runID != globalRunID)
            {
                return;
            }

            // Swap buffers
            var tempBufferReference = secondPreviousBuffer;

            secondPreviousBuffer = previousBuffer;
            previousBuffer = currentBuffer;
            currentBuffer = tempBufferReference;

            // Compute
            var rowDeltas = new List<int>() { -1, -1, -1, 0, 0, 1, 1, 1 };
            var columnDeltas = new List<int>() { -1, 0, 1, -1, 1, -1, 0, 1 };
            var currentBufferIndex = 0;

            for (var currentBufferRow = 0; currentBufferRow < numRowsOfCells; ++currentBufferRow)
            {

                for (var currentBufferCol = 0; currentBufferCol < numColumnsOfCells; ++currentBufferCol)
                {
                    var numLiveNeighbours = 0;

                    for (var neighbour = 0; neighbour < numNeighbours; ++neighbour)
                    {
                        var previousBufferRow = currentBufferRow + rowDeltas[neighbour];
                        var previousBufferCol = currentBufferCol + columnDeltas[neighbour];

                        if (previousBufferRow < 0)
                        {
                            previousBufferRow = numRowsOfCells - 1;
                        }
                        else if (previousBufferRow >= numRowsOfCells)
                        {
                            previousBufferRow = 0;
                        }

                        if (previousBufferCol < 0)
                        {
                            previousBufferCol = numColumnsOfCells - 1;
                        }
                        else if (previousBufferCol >= numColumnsOfCells)
                        {
                            previousBufferCol = 0;
                        }

                        var previousBufferIndex = previousBufferRow * numColumnsOfCells + previousBufferCol;

                        if (previousBuffer[previousBufferIndex] > 0)
                        {
                            ++numLiveNeighbours;
                        }
                    }

                    var cellAge = previousBuffer[currentBufferIndex];   // This may look confusing, but it is correct.
                    var cellShallBeAlive = false;

                    if (rules3_4Life)
                    {
                        cellShallBeAlive = (numLiveNeighbours == 3 || numLiveNeighbours == 4);
                    }
                    else
                    {
                        cellShallBeAlive = ((numLiveNeighbours == 2 && cellAge > 0) || numLiveNeighbours == 3);
                    }
            
                    if (cellShallBeAlive)
                    {

                        if (cellAge < maxCellAge)
                        {
                            ++cellAge;
                        }
                    }
                    else
                    {
                        cellAge = 0;
                    }

                    currentBuffer[currentBufferIndex] = cellAge;
                    ++currentBufferIndex;
                }
            }

            // Display
            displayCurrentBuffer();

            ++generationNumber;
            displayGenerationNumber();

            if (!singleStep && autoStop && buffersAreEqual(currentBuffer, secondPreviousBuffer))    // Compare generation n with generation n - 2 because flippers have a period of 2.
            {
                dispatcherTimer.Stop();
            }
        }

        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            computeAndDisplayNextGeneration(globalRunID, false);
        }

        // **** Event Handlers ****

        public void onCanvasClick(int x, int y)
        {
            var cellRow = y / cellSize;
            var cellCol = x / cellSize;

            if (cellRow < 0 || cellRow >= numRowsOfCells || cellCol < 0 || cellCol >= numColumnsOfCells)
            {
                return;
            }

            var cellIndex = cellRow * numColumnsOfCells + cellCol;

            currentBuffer[cellIndex] = (currentBuffer[cellIndex] > 0) ? 0 : 1;
            displayCurrentBuffer();
        }

        public void rbRules_onClick(bool rulesParam)
        {
            rules3_4Life = rulesParam;
        }

        public void ddlDelay_onChange(int delayParam)
        {
            intergenerationalDelay = delayParam;
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, intergenerationalDelay);
        }

        public void cbAutoStop_onChange(bool autoStopParam)
        {
            autoStop = autoStopParam;
        }

        public void btnClear_onClick()
        {
            resetGenerationNumber();
            clearCurrentBuffer();
            displayCurrentBuffer();
        }

        public void btnRandom_onClick()
        {
            resetGenerationNumber();
            randomizeCurrentBuffer();
            displayCurrentBuffer();
        }

        public void btnRemember_onClick()
        {

            if (!copyBuffer(currentBuffer, memoryBuffer))
            {
                return;
            }

            rememberAutoStop = autoStop;
            rememberRules3_4Life = rules3_4Life;
            //alert("The current pattern has now been remembered.");
        }

        public void btnRecall_onClick()
        {

            if (!copyBuffer(memoryBuffer, currentBuffer))
            {
                return;
            }

            resetGenerationNumber();
            setAutoStop(rememberAutoStop);
            setRules(rememberRules3_4Life);
            displayCurrentBuffer();
        }

        public void btnStep_onClick()
        {
            computeAndDisplayNextGeneration(globalRunID, true);
        }

        public void btnGo_onClick()
        {
            dispatcherTimer.Start();
        }

        public void btnStop_onClick()
        {
            dispatcherTimer.Stop();
        }

        public void LoadPattern(XmlDocument xmlDoc)
        {

            if (xmlDoc == null)
            {
                //alert("btnLoad_onClick_callback failed: xmlDoc is null");
                return;
            }

            var patternWidthNode = xmlDoc.SelectSingleNode("/pattern/width");
            var patternHeightNode = xmlDoc.SelectSingleNode("/pattern/height");
            var rows = xmlDoc.SelectNodes("/pattern/rows/row");

            if (patternWidthNode == null || patternHeightNode == null || rows == null)
            {
                return;
            }

            var patternWidth = int.Parse(patternWidthNode.InnerText);
            var patternHeight = int.Parse(patternHeightNode.InnerText);

            if (patternWidth > numColumnsOfCells)
            {
                patternWidth = numColumnsOfCells;
            }

            if (patternHeight > numRowsOfCells)
            {
                patternHeight = numRowsOfCells;
            }

            if (patternHeight > rows.Count)
            {
                patternHeight = rows.Count;
            }

            var startRow = (numRowsOfCells - patternHeight) / 2;
            var startCol = (numColumnsOfCells - patternWidth) / 2;
            var startIndex = startRow * numColumnsOfCells + startCol;
            var r = 0;

            resetGenerationNumber();

            var xmlElement_rules = xmlDoc.SelectSingleNode("/pattern/rules");

            if (xmlElement_rules != null && xmlElement_rules.InnerText == "3-4")
            {
                setRules(true);
            }

            var xmlElement_autoStop = xmlDoc.SelectSingleNode("/pattern/auto-stop");

            if (xmlElement_autoStop != null && xmlElement_autoStop.InnerText == "false")
            {
                setAutoStop(false);
            }

            clearCurrentBuffer();

            foreach (XmlNode row in rows)
            {

                if (r >= patternHeight)
                {
                    return;
                }

                var rowHexString = row.InnerText;
                var currentIndex = startIndex;
                var nybble = 0;
                var mask = 0;
                var currentStringIndex = 0;
                var nextStringIndex = 0;

                for (var c = 0; c < patternWidth; ++c)
                {

                    if (mask < 1)
                    {
                        currentStringIndex = nextStringIndex;
                        ++nextStringIndex;

                        if (currentStringIndex >= rowHexString.Length)
                        {
                            break;
                        }

                        var nybbleAsString = rowHexString[currentStringIndex].ToString();

                        nybble = int.Parse(nybbleAsString, NumberStyles.AllowHexSpecifier);
                        mask = 8;
                    }

                    if ((nybble & mask) != 0)
                    {
                        currentBuffer[currentIndex] = 1;
                    }

                    ++currentIndex;
                    mask /= 2;
                }

                startIndex += numColumnsOfCells;
                ++r;
            }

            displayCurrentBuffer();
        }
    }
}
