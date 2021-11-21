// See https://aka.ms/new-console-template for more information

using System;
using Tesseract;
using WFInfo.Services.Tests;

Console.WriteLine("Hello, World!");
var test = new RewardHelperTests();
await test.Test();
