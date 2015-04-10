﻿using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using ExtensibleILRewriter.Extensions;
using System;
using ExtensibleILRewriter.MethodProcessors.ArgumentHandling;

namespace ExtensibleILRewriter.MethodProcessors.Contracts
{
    public class NotNullAttributeProcessor : ComponentProcessor<MethodDefinition, NotNullAttributeProcessor.Configuration>
    {
        private readonly static string notNullAttributeFullName = typeof(NotNullAttribute).FullName;
        private readonly Dictionary<TypeReference, IArgumentHandlingCodeInjector> codeInjectorsCache = new Dictionary<TypeReference, IArgumentHandlingCodeInjector>();

        public NotNullAttributeProcessor([NotNull]Configuration configuration, [NotNull]ILogger logger)
            : base(configuration, logger)
        {
        }

        public override void Process(MethodDefinition method)
        {
            foreach (var parameter in method.Parameters)
            {
                if (parameter.CustomAttributes.Any(a => a.AttributeType.FullName == notNullAttributeFullName))
                {
                    if (parameter.ParameterType.IsValueType)
                    {
                        logger.LogErrorWithSource(method, "Parameter '\{parameter.Name}' of method '\{method.FullName}' cannot be non-nullable because it is a value type.");
                        continue;
                    }

                    if (!method.HasBody)
                    {
                        logger.LogErrorWithSource(method, "Method '\{method.FullName}' does not have body and cannot be rewritten.");
                        continue;
                    }

                    var codeInjector = GetCodeInjector(method.Module, parameter.ParameterType);
                    codeInjector.Inject(method, parameter, logger);
                }
            }
        }

        private IArgumentHandlingCodeInjector GetCodeInjector(ModuleDefinition module, TypeReference ArgumentType)
        {
            IArgumentHandlingCodeInjector codeInjector;

            if (!codeInjectorsCache.TryGetValue(ArgumentType, out codeInjector))
            {
                var parameterClrType = Type.GetType(ArgumentType.FullName);
                var codeProviderType = typeof(NotNullArgumentHandligCodeProvider<>).MakeGenericType(parameterClrType);
                var codeInjectorType = typeof(ArgumentHandligCodeInjector<>).MakeGenericType(parameterClrType);
                var codeProvider = Activator.CreateInstance(codeProviderType, configuration.HandlingType, configuration.HandlingInstanceType);

                codeInjector = (IArgumentHandlingCodeInjector)Activator.CreateInstance(codeInjectorType, new object[] { module, codeProvider });

                codeInjectorsCache.Add(ArgumentType, codeInjector);
            }

            return codeInjector;
        }

        public class Configuration : ComponentProcessorConfiguration
        {
            private ArgumentHandlingType handlingType;
            public ArgumentHandlingType HandlingType { get { return handlingType; } }

            public TypeDefinition HandlingInstanceType { get; private set; }

            public override void LoadFromProperties(ComponentProcessorProperties properties, TypeAliasResolver typeAliasResolver)
            {
                const string HandligTypeElementName = "HandlingType";
                CheckIfContainsProperty(properties, HandligTypeElementName);
                if (!Enum.TryParse<ArgumentHandlingType>(properties.GetProperty(HandligTypeElementName), out handlingType))
                {
                    throw new InvalidOperationException("Unable to parse handling type property of \{nameof(NotNullAttributeProcessor)} processor. Value: '\{properties.GetProperty(HandligTypeElementName)}'.");
                }

                switch (handlingType)
                {
                    case ArgumentHandlingType.CallStaticHandling:
                        HandlingInstanceType = null;
                        break;
                    case ArgumentHandlingType.CallInstanceHandling:
                        const string HandlingInstanceTypeAliasElementName = "HandlingInstanceTypeAlias";
                        CheckIfContainsProperty(properties, HandlingInstanceTypeAliasElementName);
                        HandlingInstanceType = typeAliasResolver.Resolve(properties.GetProperty(HandlingInstanceTypeAliasElementName));
                        break;
                    default:
                        throw new NotImplementedException("Unknown argument handling type: '\{handlingType}'.");
                }
            }
        }
    }
}