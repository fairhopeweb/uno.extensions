﻿using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Core.Tests.Utils;

namespace Uno.Extensions.Core.Tests.PropertySelector;

[TestClass]
public class Given_PS0001
{
	[TestMethod]
	public async Task When_UsingMethod()
	{
		var compilation = GenerationTestHelper.CreateCompilationWithAnalyzers($@"
			using Uno.Extensions.Edition;
			using System.Runtime.CompilerServices;

			namespace TestNamespace;

			public record Entity(string Value);

			public class SUTClass
			{{
				public void Test()
				{{
					SUTMethod(e => e.ToString());
				}}

				public void SUTMethod(PropertySelector<Entity, string> selector, [CallerFilePath] string path = """", [CallerLineNumber] int line = -1)
				{{
				}}
			}}
			");

		var diagnostics = await compilation.GetAnalyzerDiagnosticsAsync();
		diagnostics.Length.Should().Be(1);

		var pathDiag = diagnostics[0];
		pathDiag.Id.Should().Be("PS0001");
		pathDiag.Location.GetLineSpan().StartLinePosition.Line.Should().Be(12);
		pathDiag.Location.GetLineSpan().StartLinePosition.Character.Should().Be(20);

		var expectedCode = @"//----------------------
// <auto-generated>
//	Generated by the PropertySelectorsGenerationTool v1. DO NOT EDIT!
//	Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//----------------------
#pragma warning disable

namespace Tests.__PropertySelectors
{
	/// <summary>
	/// Auto registration class for PropertySelector used in <global namespace>.
	/// </summary>
	[global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Never)]
	internal static class _13_5
	{
		/// <summary>
		/// Register the value providers for the PropertySelectors used to invoke 'SUTMethod'
		/// in  on line 13.
		/// </summary>
		/// <remarks>
		/// This method is flagged with the [ModuleInitializerAttribute] which means that it will be invoked by the runtime when the module is being loaded.
		/// You should not have to use it at any time.
		/// </remarks>
		[global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Never)]
		[global::System.Runtime.CompilerServices.ModuleInitializerAttribute]
		internal static void Register()
		{
			global::Uno.Extensions.Edition.PropertySelectors.Register(
				@""selector13"",
				new global::Uno.Extensions.Edition.ValueAccessor<TestNamespace.Entity, string>(
			"""",
			entity => entity,
			(current_0, updated_0) =>
			{



				return updated_0;
			}));
		}
	}
}";
		GenerationTestHelper.RunGeneratorTwice(
			compilation.Compilation,
			run1 => GenerationTestHelper.AssertRunReason(run1, IncrementalStepRunReason.New),
			run2 => GenerationTestHelper.AssertRunReason(run2, IncrementalStepRunReason.Cached),
			expectedCode);
	}

	[TestMethod]
	public async Task When_UsingConstant()
	{
		var compilation = GenerationTestHelper.CreateCompilationWithAnalyzers($@"
			using Uno.Extensions.Edition;
			using System.Runtime.CompilerServices;

			namespace TestNamespace;

			public record Entity(string Value);

			public class SUTClass
			{{
				public void Test()
				{{
					SUTMethod(e => ""Value"");
				}}

				public void SUTMethod(PropertySelector<Entity, string> selector, [CallerFilePath] string path = """", [CallerLineNumber] int line = -1)
				{{
				}}
			}}
			");

		var diagnostics = await compilation.GetAnalyzerDiagnosticsAsync();
		diagnostics.Length.Should().Be(1);

		var pathDiag = diagnostics[0];
		pathDiag.Id.Should().Be("PS0001");
		pathDiag.Location.GetLineSpan().StartLinePosition.Line.Should().Be(12);
		pathDiag.Location.GetLineSpan().StartLinePosition.Character.Should().Be(20);

		var expectedCode = @"//----------------------
// <auto-generated>
//	Generated by the PropertySelectorsGenerationTool v1. DO NOT EDIT!
//	Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//----------------------
#pragma warning disable

namespace Tests.__PropertySelectors
{
	/// <summary>
	/// Auto registration class for PropertySelector used in <global namespace>.
	/// </summary>
	[global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Never)]
	internal static class _13_5
	{
		/// <summary>
		/// Register the value providers for the PropertySelectors used to invoke 'SUTMethod'
		/// in  on line 13.
		/// </summary>
		/// <remarks>
		/// This method is flagged with the [ModuleInitializerAttribute] which means that it will be invoked by the runtime when the module is being loaded.
		/// You should not have to use it at any time.
		/// </remarks>
		[global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Never)]
		[global::System.Runtime.CompilerServices.ModuleInitializerAttribute]
		internal static void Register()
		{
			global::Uno.Extensions.Edition.PropertySelectors.Register(
				@""selector13"",
				new global::Uno.Extensions.Edition.ValueAccessor<TestNamespace.Entity, string>(
			"""",
			entity => entity,
			(current_0, updated_0) =>
			{



				return updated_0;
			}));
		}
	}
}";

		GenerationTestHelper.RunGeneratorTwice(
			compilation.Compilation,
			run1 => GenerationTestHelper.AssertRunReason(run1, IncrementalStepRunReason.New),
			run2 => GenerationTestHelper.AssertRunReason(run2, IncrementalStepRunReason.Cached),
			expectedCode);
	}
}
