using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace PriceSheetGenerator
{
    class Program
    {
        private static readonly CancellationTokenSource m_CancellationToken = new();
        private static async Task Main(string[] args)
        {
            const string cycleCountArgName = "-cycle_count";
            const string unlimitedCycleString = "unlimited";
            var cycleCountFound = false;
            var cycleCount = 1;

            const string cycleTimeArgName = "-cycle_time";
            var cycleTimeFound = false;
            var cycleTimeMs = 1000 * 60; // 60 

            const string cycleStepsArgName = "-cycle_steps";
            var cycleStepsFound = false;
            var cycleSteps = 3;

            const string saveIntervalArgName = "-save_interval";
            var saveIntervalFound = false;
            var saveInterval = 0; // cycles between state and output being saved to file

            const string backupArgName = "-backup";
            var backupFound = false;
            var backup = true;

            const string internalDataDirArgName = "-data_dir";
            var internalDataDirFound = false;
            string? internalDataDir = null;

            const string outputArgName = "-output";
            var outputFound = false;
            string? outputPath = null;

            const string statusIntervalArgName = "-status_interval";
            var statusIntervalFound = false;
            var statusInterval = 100; // cycles between status report

            const string helpArgName = "-help";

            for (int i = 0; i < args.Length; i++)
            {
                var currArg = args[i];
                var nextArg = (i + 1) >= args.Length ? null : args[i + 1];

                if (currArg.Equals(helpArgName, StringComparison.InvariantCultureIgnoreCase))
                {
                    Console.WriteLine("Price data json generator for WFInfo. Usage: " + Environment.NewLine +
                        helpArgName + " : This help prompt" + Environment.NewLine +
                        cycleCountArgName + " (number or " + unlimitedCycleString + ") :Cycles to perform before exiting. Default 1." + Environment.NewLine +
                        cycleTimeArgName + " (number in milliseconds) : Time delay between cycles. Default 60 seconds." + Environment.NewLine +
                        cycleStepsArgName + " (number) : Item fetches per cycle. Default 3." + Environment.NewLine + 
                        saveIntervalArgName + " (number) : Cycles between each save, covering output and internal state. Default 0." + Environment.NewLine +
                        backupArgName + " (bool) : Previous content moved to .bak instead of deleted on save. Default true." + Environment.NewLine +
                        statusIntervalArgName + " (number) : Cycles between status reports. Default 100." + Environment.NewLine +
                        internalDataDirArgName + " (directory path) : Directory to save internal state between runs. Default executable dir" + Environment.NewLine +
                        outputArgName + " (file path) : File to save output to. Default (executable dir)/prices.json.");
                    Environment.Exit(1);
                }

                if (currArg.Equals(cycleCountArgName, StringComparison.InvariantCultureIgnoreCase))
                {
                    MarkArgumentFound(cycleCountArgName, ref cycleCountFound);

                    if (nextArg is null)
                    {
                        ExitParameterRequired(cycleCountArgName);
                    }

                    if (nextArg.Equals(unlimitedCycleString, StringComparison.InvariantCultureIgnoreCase))
                    {
                        cycleCount = int.MaxValue;
                    }
                    else if (!int.TryParse(nextArg, CultureInfo.InvariantCulture, out cycleCount) || cycleCount <= 0)
                    {
                        ExitUnrecognizedParameter(cycleCountArgName, nextArg);
                    }
                    i++; // increment due to parameter
                    continue;
                }

                if (currArg.Equals(cycleTimeArgName, StringComparison.InvariantCultureIgnoreCase))
                {
                    MarkArgumentFound(cycleTimeArgName, ref cycleTimeFound);

                    if (nextArg is null)
                    {
                        ExitParameterRequired(cycleTimeArgName);
                    }

                    if (!int.TryParse(nextArg, CultureInfo.InvariantCulture, out cycleTimeMs) || cycleTimeMs < 0)
                    {
                        ExitUnrecognizedParameter(cycleTimeArgName, nextArg);
                    }
                    i++; // increment due to parameter
                    continue;
                }

                if (currArg.Equals(cycleStepsArgName, StringComparison.InvariantCultureIgnoreCase))
                {
                    MarkArgumentFound(cycleStepsArgName, ref cycleStepsFound);

                    if (nextArg is null)
                    {
                        ExitParameterRequired(cycleStepsArgName);
                    }

                    if (!int.TryParse(nextArg, CultureInfo.InvariantCulture, out cycleSteps) || cycleSteps <= 0)
                    {
                        ExitUnrecognizedParameter(cycleStepsArgName, nextArg);
                    }
                    i++; // increment due to parameter
                    continue;
                }

                if (currArg.Equals(saveIntervalArgName, StringComparison.InvariantCultureIgnoreCase))
                {
                    MarkArgumentFound(saveIntervalArgName, ref saveIntervalFound);

                    if (nextArg is null)
                    {
                        ExitParameterRequired(saveIntervalArgName);
                    }

                    if (!int.TryParse(nextArg, CultureInfo.InvariantCulture, out saveInterval) || saveInterval < 0)
                    {
                        ExitUnrecognizedParameter(saveIntervalArgName, nextArg);
                    }
                    i++; // increment due to parameter
                    continue;
                }

                if (currArg.Equals(statusIntervalArgName, StringComparison.InvariantCultureIgnoreCase))
                {
                    MarkArgumentFound(statusIntervalArgName, ref statusIntervalFound);

                    if (nextArg is null)
                    {
                        ExitParameterRequired(statusIntervalArgName);
                    }

                    if (!int.TryParse(nextArg, CultureInfo.InvariantCulture, out statusInterval) || statusInterval < 0)
                    {
                        ExitUnrecognizedParameter(statusIntervalArgName, nextArg);
                    }
                    i++; // increment due to parameter
                    continue;
                }

                if (currArg.Equals(internalDataDirArgName, StringComparison.InvariantCultureIgnoreCase))
                {
                    MarkArgumentFound(internalDataDirArgName, ref internalDataDirFound);

                    if (nextArg is null)
                    {
                        ExitParameterRequired(internalDataDirArgName);
                    }

                    internalDataDir = nextArg;
                    i++; // increment due to parameter
                    continue;
                }

                if (currArg.Equals(outputArgName, StringComparison.InvariantCultureIgnoreCase))
                {
                    MarkArgumentFound(outputArgName, ref outputFound);

                    if (nextArg is null)
                    {
                        ExitParameterRequired(outputArgName);
                    }

                    outputPath = nextArg;
                    i++; // increment due to parameter
                    continue;
                }

                if (currArg.Equals(backupArgName, StringComparison.InvariantCultureIgnoreCase))
                {
                    MarkArgumentFound(backupArgName, ref backupFound);

                    if (nextArg is null)
                    {
                        ExitParameterRequired(backupArgName);
                    }

                    if (!bool.TryParse(nextArg, out backup))
                    {
                        ExitUnrecognizedParameter(backupArgName, nextArg);
                    }
                    i++; // increment due to parameter
                    continue;
                }

                Console.WriteLine("Unknown argument, see -help for usage. Argument: " + currArg);
                Environment.Exit(1);
            }

            if (internalDataDir is null)
            {
                var processPath = Environment.ProcessPath;
                var processDir = Path.GetDirectoryName(processPath) ?? throw new Exception("processDir unknown");

                internalDataDir = processDir;
            }

            if (!Directory.Exists(internalDataDir))
            {
                Console.WriteLine("Value for argument " + internalDataDirArgName + " does not refer to an existing directory. Value: " + internalDataDir);
                Environment.Exit(1);
            }


            if (outputPath is null)
            {
                var processPath = Environment.ProcessPath;
                var processDir = Path.GetDirectoryName(processPath) ?? throw new Exception("processDir unknown");

                outputPath = Path.Combine(processDir, "prices.json");
            }

            var fullOutputPath = Path.GetFullPath(outputPath);
            var outputDir = Path.GetDirectoryName(fullOutputPath);
            var fileName = Path.GetFileName(fullOutputPath);

            if (Directory.Exists(fullOutputPath) || string.IsNullOrEmpty(fileName))
            {
                Console.WriteLine("Value for argument " + outputArgName + " refers to a (possibly existing) directory, expected a file name. Value: " + fullOutputPath);
                Environment.Exit(1);
            }

            if (string.IsNullOrEmpty(outputDir) || !Directory.Exists(outputDir))
            {
                Console.WriteLine("Value for argument " + outputArgName + " is not within an existing directory. Value: " + fullOutputPath);
                Environment.Exit(1);
            }

            Console.WriteLine("Price data json generator for WFInfo starting with following settings: " + Environment.NewLine +
                        cycleCountArgName + " " + (cycleCount == int.MaxValue ? unlimitedCycleString : cycleCount.ToString()) + Environment.NewLine +
                        cycleTimeArgName + " " + cycleTimeMs + Environment.NewLine +
                        cycleStepsArgName + " " + cycleSteps + Environment.NewLine +
                        saveIntervalArgName + " " + saveInterval + Environment.NewLine +
                        backupArgName + " " + backup + Environment.NewLine +
                        statusIntervalArgName + " " + statusInterval + Environment.NewLine +
                        internalDataDirArgName + " " + internalDataDir + Environment.NewLine +
                        outputArgName + " " + outputPath);

            // Try to load state. If fail, make clean

            if (!PriceSheetInternal.TryLoad(internalDataDir, out var sheet))
            {
                Console.WriteLine("Failed to load previous state, starting clean");
                sheet = new PriceSheetInternal();
            }

            sheet.KeepBackup = backup;

            // threading approach: 
            // semaphore for processor
            // cancellation token for ctrl+c. Checked before and after each step in cycle
            // async used to let ctrl+c gracefully stop via cancellation token

            var mainTask = StartLoop(sheet, internalDataDir, outputPath, cycleCount, cycleTimeMs, cycleSteps, saveInterval, statusInterval, m_CancellationToken.Token);

            Console.CancelKeyPress += OnCancel;

            await mainTask.ConfigureAwait(false);

            Console.CancelKeyPress -= OnCancel;
        }

        private static void OnCancel(object? sender, ConsoleCancelEventArgs e)
        {
            if (m_CancellationToken.IsCancellationRequested)
            {
                Console.WriteLine("Second cancellation received, allowing hard exit");
                return;
            }

            Console.WriteLine("Cancellation received, attempting graceful exit"); // "graceful" exit
            m_CancellationToken.Cancel();
            e.Cancel = true;
        }

        private static async Task StartLoop(PriceSheetInternal sheet, string internalDataDir, string outputPath, int cycleCount, int cycleTimeMs, int cycleSteps, int saveInterval, int statusInterval, CancellationToken cancellationToken)
        {
            var acquired = await sheet.Sema.WaitAsync(0, cancellationToken).ConfigureAwait(false);
            if (!acquired)
            {
                return; // cancellation occurred before work started
            }

            var currentCycle = 0;
            var lastSaveCycle = -1;
            var fetchFailureCount = 0;
            var retryCount = 0;
            const int retryCap = 3;
            try
            {
                var continueLoop = true;
                while (continueLoop)
                {
                    // this may theoretically throw in an unchecked context, but even at 3 cycles per second that'd take over 20 years without any downtime
                    currentCycle++;

                    cancellationToken.ThrowIfCancellationRequested();

                    if (sheet.ShouldRepopulate())
                    {
                        var retryAttempts = 0;
                        JsonNode? data = null;

                        while (retryAttempts < retryCap && data is null)
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            data = await WfmClient.Instance.RequestItemList(cancellationToken).ConfigureAwait(false);

                            if (data is null)
                            {
                                retryAttempts++;
                            }
                        }
                        
                        if (data is not null && data.GetValueKind() is JsonValueKind.Object)
                        {
                            var dataObj = data.AsObject();
                            sheet.PopulateFromWfm(dataObj);

                            if (retryAttempts != 0)
                            {
                                retryCount++;
                            }
                        }
                        else
                        {
                            fetchFailureCount++;
                        }
                    }

                    for (var i = 0; i < cycleSteps; i++)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        if (sheet.TryStep(out var item))
                        {
                            var retryAttempts = 0;
                            JsonNode? data = null;

                            while (retryAttempts < retryCap && data is null)
                            {
                                cancellationToken.ThrowIfCancellationRequested();
                                data = await WfmClient.Instance.RequestItemStatistics(item.Slug, cancellationToken).ConfigureAwait(false);

                                if (data is null)
                                {
                                    retryAttempts++;
                                }
                            }

                            if (data is not null && data.GetValueKind() is JsonValueKind.Object)
                            {
                                var dataObj = data.AsObject();
                                item.ParseWfmStatistics(dataObj);

                                if (retryAttempts != 0)
                                {
                                    retryCount++;
                                }
                            }
                            else
                            {
                                fetchFailureCount++;
                            }
                        }
                        else
                        {
                            // early cycle stop due to lacking item to check. Should basically never happen
                            break;
                        }
                    }

                    cancellationToken.ThrowIfCancellationRequested();

                    continueLoop = currentCycle < cycleCount || cycleCount == int.MaxValue;

                    if (!continueLoop || saveInterval == 0 || currentCycle % saveInterval == 0)
                    {
                        sheet.Save(internalDataDir, outputPath);
                        lastSaveCycle = currentCycle;
                    }

                    if (statusInterval == 0 || currentCycle % statusInterval == 0)
                    {
                        sheet.GetItemStates(out var totalCount, out var deletedCount, out var missingStatsCount);
                        Console.WriteLine("Status: Current cycle is " + currentCycle + ", last save was cycle " + lastSaveCycle + Environment.NewLine + 
                            "Total item count is " + totalCount + ", of which " + deletedCount + " are deleted and " + missingStatsCount + " are missing statistics" + Environment.NewLine +
                            "WFM data fetch required retry but succeeded in " + retryCount + " instances, and completely failed " + fetchFailureCount + " times");
                    }

                    if (continueLoop)
                    {
                        await Task.Delay(cycleTimeMs, cancellationToken).ConfigureAwait(false);
                    }
                }
            }
            catch (TaskCanceledException ex)
            {

            }
            catch (OperationCanceledException ex)
            {

            }
            finally
            {
                Console.WriteLine("Loop exiting on cycle " + currentCycle + ". Last save occurred on cycle " + lastSaveCycle);
                sheet.Sema.Release();
            }
        }

        [DoesNotReturn]
        private static void ExitParameterRequired(string argument)
        {
            Console.Write("Argument " + argument + " requires parameter. See -help for usage");
            Environment.Exit(1);
        }

        private static void ExitUnrecognizedParameter(string argument, string parameter)
        {
            Console.Write("Argument " + argument + " received unrecognized or unsupported parameter. See -help for usage. Parameter: " + parameter);
            Environment.Exit(1);
        }

        private static void MarkArgumentFound(string argument, ref bool foundBool)
        {
            if (foundBool)
            {
                Console.Write("Argument " + argument + " received multiple times");
                Environment.Exit(1);
            }

            foundBool = true;
        }
    }
}