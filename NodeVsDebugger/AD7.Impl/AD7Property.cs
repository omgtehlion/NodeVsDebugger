using System;
using System.Linq;
using Microsoft.VisualStudio.Debugger.Interop;

namespace NodeVsDebugger
{
    using DIF = enum_DEBUGPROP_INFO_FLAGS;

    // An implementation of IDebugProperty2
    // This interface represents a stack frame property, a program document property, or some other property. 
    // The property is usually the result of an expression evaluation. 
    //
    // IDebugExpression2 represents a succesfully parsed expression to the debugger. 
    // It is returned as a result of a successful call to IDebugExpressionContext2.ParseText
    // It allows the debugger to obtain the values of an expression in the debuggee. 
    // For the purposes of this sample, this means obtaining the values of locals and parameters from a stack frame.
    class AD7Property : IDebugExpression2, IDebugProperty2, IDebugProperty3
    {
        private Property m_variableInformation;

        public AD7Property(Property vi)
        {
            m_variableInformation = vi;
        }

        // Construct a DEBUG_PROPERTY_INFO representing this local or parameter.
        public DEBUG_PROPERTY_INFO ConstructDebugPropertyInfo(DIF dwFields)
        {
            var propertyInfo = new DEBUG_PROPERTY_INFO();

            if (dwFields.HasFlag(DIF.DEBUGPROP_INFO_FULLNAME)) {
                propertyInfo.bstrFullName = m_variableInformation.m_fullName;
                propertyInfo.dwFields |= DIF.DEBUGPROP_INFO_FULLNAME;
            }

            if (dwFields.HasFlag(DIF.DEBUGPROP_INFO_NAME)) {
                propertyInfo.bstrName = m_variableInformation.m_name;
                propertyInfo.dwFields |= DIF.DEBUGPROP_INFO_NAME;
            }

            if (dwFields.HasFlag(DIF.DEBUGPROP_INFO_TYPE)) {
                propertyInfo.bstrType = m_variableInformation.m_typeName;
                propertyInfo.dwFields = DIF.DEBUGPROP_INFO_TYPE;
            }

            if (dwFields.HasFlag(DIF.DEBUGPROP_INFO_VALUE)) {
                propertyInfo.bstrValue = m_variableInformation.m_value;
                propertyInfo.dwFields = DIF.DEBUGPROP_INFO_VALUE;
            }

            if (dwFields.HasFlag(DIF.DEBUGPROP_INFO_ATTRIB)) {
                // The sample does not support writing of values displayed in the debugger, so mark them all as read-only.
                propertyInfo.dwAttrib = 0;//                enum_DBG_ATTRIB_FLAGS.DBG_ATTRIB_VALUE_READONLY;
                if (m_variableInformation.IsString)
                    propertyInfo.dwAttrib |= enum_DBG_ATTRIB_FLAGS.DBG_ATTRIB_VALUE_RAW_STRING;
                if (m_variableInformation.HasChildren)
                    propertyInfo.dwAttrib |= enum_DBG_ATTRIB_FLAGS.DBG_ATTRIB_OBJ_IS_EXPANDABLE;
                if (m_variableInformation.HasAccessor)
                    propertyInfo.dwAttrib |= enum_DBG_ATTRIB_FLAGS.DBG_ATTRIB_PROPERTY;
                if (m_variableInformation.IsPrivate)
                    propertyInfo.dwAttrib |= enum_DBG_ATTRIB_FLAGS.DBG_ATTRIB_ACCESS_PRIVATE;
                if (m_variableInformation.IsMethod)
                    propertyInfo.dwAttrib |= enum_DBG_ATTRIB_FLAGS.DBG_ATTRIB_METHOD;
                //propertyInfo.dwAttrib |= enum_DBG_ATTRIB_FLAGS.DBG_ATTRIB_VALUE_SIDE_EFFECT;
                propertyInfo.dwFields |= DIF.DEBUGPROP_INFO_ATTRIB;
            }

            // If the debugger has asked for the property, or the property has children (meaning it is a pointer in the sample)
            // then set the pProperty field so the debugger can call back when the chilren are enumerated.
            if ((dwFields.HasFlag(DIF.DEBUGPROP_INFO_PROP)) ||
                (m_variableInformation.HasChildren)) {
                propertyInfo.pProperty = this;
                propertyInfo.dwFields |= DIF.DEBUGPROP_INFO_PROP;
            }

            return propertyInfo;
        }

        #region IDebugProperty2 Members

        // Enumerates the children of a property. This provides support for dereferencing pointers, displaying members of an array, or fields of a class or struct.
        // The sample debugger only supports pointer dereferencing as children. This means there is only ever one child.
        public int EnumChildren(DIF dwFields, uint dwRadix, ref System.Guid guidFilter, enum_DBG_ATTRIB_FLAGS dwAttribFilter, string pszNameFilter, uint dwTimeout, out IEnumDebugPropertyInfo2 ppEnum)
        {
            ppEnum = null;
            try {
                ppEnum = new AD7PropertyEnum(m_variableInformation
                    .GetChildren().OrderBy(v => v.m_name)
                    .Select(v => new AD7Property(v).ConstructDebugPropertyInfo(dwFields))
                );
                return Constants.S_OK;
            } catch {
                return Constants.S_FALSE;
            }
        }

        // Returns the property that describes the most-derived property of a property
        // This is called to support object oriented languages. It allows the debug engine to return an IDebugProperty2 for the most-derived 
        // object in a hierarchy. This engine does not support this.
        public int GetDerivedMostProperty(out IDebugProperty2 ppDerivedMost)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        // This method exists for the purpose of retrieving information that does not lend itself to being retrieved by calling the IDebugProperty2::GetPropertyInfo 
        // method. This includes information about custom viewers, managed type slots and other information.
        // The sample engine does not support this.
        public int GetExtendedInfo(ref System.Guid guidExtendedInfo, out object pExtendedInfo)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        // Returns the memory bytes for a property value.
        public int GetMemoryBytes(out IDebugMemoryBytes2 ppMemoryBytes)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        // Returns the memory context for a property value.
        public int GetMemoryContext(out IDebugMemoryContext2 ppMemory)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        // Returns the parent of a property.
        // The sample engine does not support obtaining the parent of properties.
        public int GetParent(out IDebugProperty2 ppParent)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        // Fills in a DEBUG_PROPERTY_INFO structure that describes a property.
        public int GetPropertyInfo(DIF dwFields, uint dwRadix, uint dwTimeout, IDebugReference2[] rgpArgs, uint dwArgCount, DEBUG_PROPERTY_INFO[] pPropertyInfo)
        {
            rgpArgs = null;
            pPropertyInfo[0] = ConstructDebugPropertyInfo(dwFields);
            return Constants.S_OK;
        }

        //  Return an IDebugReference2 for this property. An IDebugReference2 can be thought of as a type and an address.
        public int GetReference(out IDebugReference2 ppReference)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        // Returns the size, in bytes, of the property value.
        public int GetSize(out uint pdwSize)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        // The debugger will call this when the user tries to edit the property's values
        // the sample has set the read-only flag on its properties, so this should not be called.
        public int SetValueAsReference(IDebugReference2[] rgpArgs, uint dwArgCount, IDebugReference2 pValue, uint dwTimeout)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        // The debugger will call this when the user tries to edit the property's values in one of the debugger windows.
        // the sample has set the read-only flag on its properties, so this should not be called.
        public int SetValueAsString(string pszValue, uint dwRadix, uint dwTimeout)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        #endregion

        #region IDebugExpression2 Members

        // This method cancels asynchronous expression evaluation as started by a call to the IDebugExpression2::EvaluateAsync method.
        int IDebugExpression2.Abort()
        {
            throw new NotImplementedException();
        }

        // This method evaluates the expression asynchronously.
        // This method should return immediately after it has started the expression evaluation. 
        // When the expression is successfully evaluated, an IDebugExpressionEvaluationCompleteEvent2 
        // must be sent to the IDebugEventCallback2 event callback
        //
        // This is primarily used for the immediate window which this engine does not currently support.
        int IDebugExpression2.EvaluateAsync(enum_EVALFLAGS dwFlags, IDebugEventCallback2 pExprCallback)
        {
            throw new NotImplementedException();
        }

        // This method evaluates the expression synchronously.
        int IDebugExpression2.EvaluateSync(enum_EVALFLAGS dwFlags, uint dwTimeout, IDebugEventCallback2 pExprCallback, out IDebugProperty2 ppResult)
        {
            ppResult = this;
            return Constants.S_OK;
        }

        #endregion

        #region IDebugProperty3 Members

        int IDebugProperty3.CreateObjectID()
        {
            throw new NotImplementedException();
        }

        int IDebugProperty3.DestroyObjectID()
        {
            throw new NotImplementedException();
        }

        int IDebugProperty3.GetCustomViewerCount(out uint pcelt)
        {
            pcelt = 0;
            return Constants.E_NOTIMPL;
        }

        int IDebugProperty3.GetCustomViewerList(uint celtSkip, uint celtRequested, DEBUG_CUSTOM_VIEWER[] rgViewers, out uint pceltFetched)
        {
            throw new NotImplementedException();
        }

        int IDebugProperty3.GetStringCharLength(out uint pLen)
        {
            pLen = (uint)m_variableInformation.FullStringLength;
            return Constants.S_OK;
        }

        int IDebugProperty3.GetStringChars(uint buflen, ushort[] rgString, out uint pceltFetched)
        {
            var read = Math.Min((int)buflen, m_variableInformation.FullStringLength);
            m_variableInformation.GetFullString(read).ToCharArray().CopyTo(rgString, 0);
            pceltFetched = (uint)read;
            return Constants.S_OK;
        }

        int IDebugProperty3.SetValueAsStringWithError(string pszValue, uint dwRadix, uint dwTimeout, out string errorString)
        {
            m_variableInformation.SetValue(pszValue, out errorString);
            return errorString == null ? Constants.S_OK : Constants.S_FALSE;
        }

        #endregion

    }
}
