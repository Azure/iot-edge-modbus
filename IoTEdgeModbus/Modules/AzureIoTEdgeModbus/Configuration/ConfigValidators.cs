using AzureIoTEdgeModbus.Slave;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;

namespace AzureIoTEdgeModbus.Configuration
{
  
    public class ConfigValidators
    {

        //protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        //{
        //    return new ValidationResult(this.FormatErrorMessage("********************************"));
        //    var sc = (Dictionary<string, ModbusSlaveConfig>)value;

        //    foreach (var item in sc)
        //    {
        //        if (IPAddress.TryParse(item.Value.SlaveConnection, out IPAddress address))
        //        {
        //            //Trace.Assert(value.GetType().Equals(typeof(DateTime)));
        //            if (item.Value.TcpPort == null)
        //            {
        //                // here we are verifying whether the 2 values are equal
        //                // but you could do any custom validation you like
        //                return new ValidationResult(this.FormatErrorMessage(validationContext.DisplayName));
        //            }
        //        }
        //    }
        //    return null;
        //}

        public static List<Exception> ValidateConfig<T>(T config)
        {
            var validationErrors = new List<Exception>();
            if(config.GetType() == typeof(ModuleConfig))
            {
                var moduleConfig = config as ModuleConfig;

                foreach (var sc in moduleConfig.SlaveConfigs)
                {

                    //Validate Slave Config
                    if (IPAddress.TryParse(sc.Value.SlaveConnection, out IPAddress address))
                    {
                        string ignoreMessage = "As TCP Modbus session is used {0} field will be ignored";
                        if (sc.Value.BaudRate != null)
                            validationErrors.Add(new WarningException(String.Format(ignoreMessage, nameof(sc.Value.BaudRate))));
                        if (sc.Value.DataBits != null)
                            validationErrors.Add(new WarningException(String.Format(ignoreMessage, nameof(sc.Value.DataBits))));
                        if (sc.Value.Parity != null)
                            validationErrors.Add(new WarningException(String.Format(ignoreMessage, nameof(sc.Value.Parity))));
                        if (sc.Value.StopBits != null)
                            validationErrors.Add(new WarningException(String.Format(ignoreMessage, nameof(sc.Value.StopBits))));
                        if (sc.Value.FlowControl != null)
                            validationErrors.Add(new WarningException(String.Format(ignoreMessage, nameof(sc.Value.FlowControl))));
                    }
                    else if (sc.Value.SlaveConnection.Contains("COM") || sc.Value.SlaveConnection.Contains("tty"))
                    {
                        string missingMessage = "Value for {0} field is required!";
                        if (sc.Value.BaudRate == null)
                            validationErrors.Add(new MissingFieldException(String.Format(missingMessage, nameof(sc.Value.BaudRate))));
                        if (sc.Value.DataBits == null)
                            validationErrors.Add(new MissingFieldException(String.Format(missingMessage, nameof(sc.Value.DataBits))));
                        if (sc.Value.Parity == null)
                            validationErrors.Add(new MissingFieldException(String.Format(missingMessage, nameof(sc.Value.Parity))));
                        if (sc.Value.StopBits == null)
                            validationErrors.Add(new MissingFieldException(String.Format(missingMessage, nameof(sc.Value.StopBits))));
                        //Flow control not supported yet
                        //if (sc.Value.FlowControl == null)
                        //    validationErrors.Add(new MissingFieldException(String.Format(missingMessage, nameof(sc.Value.FlowControl))));
                    }

                    //Validate read operations
                    foreach(var ro in sc.Value.Operations)
                    {
                        if(ro.Value.IsSimpleValue == false)
                            if(!ModbusComplexValuesTypes.ValuesList.Contains(ro.Value.ValueType))
                                validationErrors.Add(new ArgumentOutOfRangeException($"Operation {ro.Value.DisplayName} has incorrect format. If not SimpleValue ValueType needs to be provided. Empty string or wrong value provided. "));
                    }

                }
                return validationErrors;
            }
            throw new NotImplementedException("Validator for this type of config not implemented");
        }
    }
}
