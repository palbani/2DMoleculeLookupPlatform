using System;
using System.Net.Http;
using System.Threading.Tasks;
using MoleculeLookup.Core.Interfaces;
using MoleculeLookup.Core.Models;
using MoleculeLookup.Core.Validation;
using MoleculeLookup.Infrastructure.Services;

namespace MoleculeLookup.Console;

/// <summary>
/// Console application for testing the Molecule Lookup Platform.
/// Demonstrates searching the ZINC20 database by SMILES string.
/// </summary>
class Program
{
    static async Task Main(string[] args)
    {
        System.Console.WriteLine("===========================================");
        System.Console.WriteLine("   2D Molecule Lookup Platform - Demo");
        System.Console.WriteLine("===========================================");
        System.Console.WriteLine();

        // Create services
        var httpClient = new HttpClient();
        var zincApiClient = new ZincApiClient(httpClient);
        var smilesConverter = new SmilesConverter();
        var validator = MoleculeValidator.CreateDefault();
        var tanimotoCalculator = new TanimotoCalculator();

        // Check if ZINC20 API is available
        System.Console.WriteLine("Checking ZINC20 API availability...");
        var isAvailable = await zincApiClient.IsAvailable();
        System.Console.WriteLine($"ZINC20 API Status: {(isAvailable ? "Online" : "Offline")}");
        System.Console.WriteLine();

        if (!isAvailable)
        {
            System.Console.WriteLine("Warning: ZINC20 API may be unavailable. Searches might fail.");
            System.Console.WriteLine();
        }

        // Demo loop
        while (true)
        {
            System.Console.WriteLine("Options:");
            System.Console.WriteLine("  1. Search by SMILES string");
            System.Console.WriteLine("  2. Search for common molecules (examples)");
            System.Console.WriteLine("  3. Validate a SMILES string");
            System.Console.WriteLine("  4. Calculate Tanimoto similarity");
            System.Console.WriteLine("  5. Exit");
            System.Console.WriteLine();
            System.Console.Write("Choose an option (1-5): ");

            var choice = System.Console.ReadLine()?.Trim();

            switch (choice)
            {
                case "1":
                    await SearchBySmiles(zincApiClient);
                    break;
                case "2":
                    await SearchExamples(zincApiClient);
                    break;
                case "3":
                    ValidateSmiles(smilesConverter, validator);
                    break;
                case "4":
                    CalculateSimilarity(tanimotoCalculator);
                    break;
                case "5":
                    System.Console.WriteLine("Goodbye!");
                    return;
                default:
                    System.Console.WriteLine("Invalid option. Please try again.");
                    break;
            }

            System.Console.WriteLine();
            System.Console.WriteLine("Press any key to continue...");
            System.Console.ReadKey();
            System.Console.Clear();
        }
    }

    static async Task SearchBySmiles(IZincApiClient zincApiClient)
    {
        System.Console.WriteLine();
        System.Console.Write("Enter SMILES string: ");
        var smiles = System.Console.ReadLine()?.Trim();

        if (string.IsNullOrEmpty(smiles))
        {
            System.Console.WriteLine("No SMILES string entered.");
            return;
        }

        System.Console.WriteLine();
        System.Console.WriteLine($"Searching ZINC20 for: {smiles}");
        System.Console.WriteLine("Please wait...");

        var result = await zincApiClient.SearchBySmiles(smiles);

        DisplaySearchResult(result);
    }

    static async Task SearchExamples(IZincApiClient zincApiClient)
    {
        var examples = new (string Name, string Smiles)[]
        {
            ("Aspirin", "CC(=O)OC1=CC=CC=C1C(=O)O"),
            ("Caffeine", "CN1C=NC2=C1C(=O)N(C(=O)N2C)C"),
            ("Ethanol", "CCO"),
            ("Glucose", "OC[C@H]1OC(O)[C@H](O)[C@@H](O)[C@@H]1O"),
            ("Benzene", "c1ccccc1")
        };

        System.Console.WriteLine();
        System.Console.WriteLine("Common molecules:");
        for (int i = 0; i < examples.Length; i++)
        {
            System.Console.WriteLine($"  {i + 1}. {examples[i].Name} ({examples[i].Smiles})");
        }
        System.Console.WriteLine();
        System.Console.Write("Choose a molecule (1-5): ");

        if (int.TryParse(System.Console.ReadLine()?.Trim(), out int choice) && choice >= 1 && choice <= 5)
        {
            var (name, smiles) = examples[choice - 1];
            System.Console.WriteLine();
            System.Console.WriteLine($"Searching ZINC20 for {name}...");
            System.Console.WriteLine("Please wait...");

            var result = await zincApiClient.SearchBySmiles(smiles);
            DisplaySearchResult(result);
        }
        else
        {
            System.Console.WriteLine("Invalid choice.");
        }
    }

    static void ValidateSmiles(ISmilesConverter smilesConverter, IMoleculeValidator validator)
    {
        System.Console.WriteLine();
        System.Console.Write("Enter SMILES string to validate: ");
        var smiles = System.Console.ReadLine()?.Trim();

        if (string.IsNullOrEmpty(smiles))
        {
            System.Console.WriteLine("No SMILES string entered.");
            return;
        }

        System.Console.WriteLine();

        // Check SMILES syntax
        var isValidSyntax = smilesConverter.IsValidSmiles(smiles);
        System.Console.WriteLine($"SMILES Syntax Valid: {isValidSyntax}");

        if (isValidSyntax)
        {
            // Parse and validate structure
            var molecule = smilesConverter.FromSmiles(smiles);
            System.Console.WriteLine($"Atoms parsed: {molecule.Atoms.Count}");
            System.Console.WriteLine($"Bonds parsed: {molecule.Bonds.Count}");

            var validationResult = validator.Validate(molecule);
            System.Console.WriteLine($"Structure Valid: {validationResult.IsValid}");

            if (!validationResult.IsValid)
            {
                System.Console.WriteLine($"Validation Error: {validationResult.ErrorMessage}");
            }

            if (validationResult.Warnings.Count > 0)
            {
                System.Console.WriteLine("Warnings:");
                foreach (var warning in validationResult.Warnings)
                {
                    System.Console.WriteLine($"  - {warning.Message}");
                }
            }
        }
    }

    static void CalculateSimilarity(ITanimotoCalculator calculator)
    {
        System.Console.WriteLine();
        System.Console.Write("Enter first SMILES string: ");
        var smiles1 = System.Console.ReadLine()?.Trim();

        System.Console.Write("Enter second SMILES string: ");
        var smiles2 = System.Console.ReadLine()?.Trim();

        if (string.IsNullOrEmpty(smiles1) || string.IsNullOrEmpty(smiles2))
        {
            System.Console.WriteLine("Both SMILES strings are required.");
            return;
        }

        var similarity = calculator.Calculate(smiles1, smiles2);
        System.Console.WriteLine();
        System.Console.WriteLine($"Tanimoto Coefficient: {similarity:F4} ({similarity * 100:F2}% similar)");

        if (similarity >= 0.85)
            System.Console.WriteLine("Interpretation: Very similar molecules");
        else if (similarity >= 0.7)
            System.Console.WriteLine("Interpretation: Moderately similar molecules");
        else if (similarity >= 0.5)
            System.Console.WriteLine("Interpretation: Somewhat similar molecules");
        else
            System.Console.WriteLine("Interpretation: Different molecules");
    }

    static void DisplaySearchResult(SearchResult result)
    {
        System.Console.WriteLine();
        System.Console.WriteLine("--- Search Result ---");
        System.Console.WriteLine($"Status: {result.Status}");
        System.Console.WriteLine($"Found: {result.IsFound}");

        if (result.SearchDuration.HasValue)
        {
            System.Console.WriteLine($"Search Time: {result.SearchDuration.Value.TotalMilliseconds:F0}ms");
        }

        if (result.IsFound && result.MoleculeData != null)
        {
            var data = result.MoleculeData;
            System.Console.WriteLine();
            System.Console.WriteLine("Molecule Data:");
            System.Console.WriteLine($"  ZINC ID: {data.ZincId}");
            System.Console.WriteLine($"  Name: {data.Name}");
            System.Console.WriteLine($"  SMILES: {data.SmilesString}");

            if (data.MolecularWeight.HasValue)
                System.Console.WriteLine($"  Molecular Weight: {data.MolecularWeight:F2} g/mol");

            if (!string.IsNullOrEmpty(data.MolecularFormula))
                System.Console.WriteLine($"  Formula: {data.MolecularFormula}");

            if (data.LogP.HasValue)
                System.Console.WriteLine($"  LogP: {data.LogP:F2}");

            if (data.HydrogenBondDonors.HasValue)
                System.Console.WriteLine($"  H-Bond Donors: {data.HydrogenBondDonors}");

            if (data.HydrogenBondAcceptors.HasValue)
                System.Console.WriteLine($"  H-Bond Acceptors: {data.HydrogenBondAcceptors}");

            if (data.RotatableBonds.HasValue)
                System.Console.WriteLine($"  Rotatable Bonds: {data.RotatableBonds}");

            if (data.RuleOfFiveCompliant.HasValue)
                System.Console.WriteLine($"  Lipinski Rule of 5: {(data.RuleOfFiveCompliant.Value ? "Compliant" : "Violation")}");

            if (!string.IsNullOrEmpty(data.ImageUrl))
                System.Console.WriteLine($"  Image URL: {data.ImageUrl}");
        }
        else if (!string.IsNullOrEmpty(result.ErrorMessage))
        {
            System.Console.WriteLine($"Error: {result.ErrorMessage}");
        }
    }
}
