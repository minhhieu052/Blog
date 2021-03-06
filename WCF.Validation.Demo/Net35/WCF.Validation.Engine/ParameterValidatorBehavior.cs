﻿using System.Collections.Generic;
using System.ServiceModel;
using System.ServiceModel.Dispatcher;

namespace WCF.Validation.Engine
{
    public class ParameterValidatorBehavior : IParameterInspector
    {
        public bool ThrowErrorOnFirstError { get; set; }
        public bool ThrowErrorAfterValidation { get; set; }

        public ParameterValidatorBehavior(bool throwErrorOnFirstError, bool throwErrorAfterValidation)
        {
            ThrowErrorOnFirstError = throwErrorOnFirstError;
            ThrowErrorAfterValidation = throwErrorAfterValidation;
        }

        public void AfterCall(string operationName, object[] outputs, object returnValue, object correlationState)
        {
        }

        public object BeforeCall(string operationName, object[] inputs)
        {
            // validate parameters before call
            var serviceIntance = OperationContext.Current.InstanceContext.GetServiceInstance() as IHasModelStateService;
            if (serviceIntance != null)
            {
                if (serviceIntance.ModelState == null)
                {
                    serviceIntance.ModelState = new ModelState();
                }
                serviceIntance.ModelState.Errors = new List<ModelError>();
                

                IEnumerable<ModelValidationResult> validationResults = new ModelValidationResult[] { };
                foreach (object input in inputs)
                {
                    if (input != null)
                    {
                        ModelMetadata metadata = ModelMetadataProviders.Current.GetMetadataForType(() => input, input.GetType());

                        validationResults = ModelValidator.GetModelValidator(metadata).Validate(null);
                        foreach (ModelValidationResult validationResult in validationResults)
                        {
                            var temp = validationResult;

                            if (ThrowErrorOnFirstError)
                            {
                                throw new FaultException<ValidationFault>(new ValidationFault(new[] { temp }), "Validation error");
                            }

                            AddModelError(serviceIntance, temp);
                        }
                    }
                }
                if (ThrowErrorAfterValidation && !serviceIntance.ModelState.IsValid)
                {
                    throw new FaultException<ValidationFault>(new ValidationFault(validationResults), "Validation error");
                }
            }
            return null;
        }

        protected virtual void AddModelError(IHasModelStateService serviceIntance, ModelValidationResult temp)
        {
            serviceIntance.ModelState.Errors.Add(new ModelError
            {
                MemberName = temp.MemberName,
                Message = temp.Message,
            });
        }
    }    
}
