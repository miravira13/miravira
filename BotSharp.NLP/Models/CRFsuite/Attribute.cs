//------------------------------------------------------------------------------
// <auto-generated />
//
// This file was automatically generated by SWIG (http://www.swig.org).
// Version 3.0.12
//
// Do not make changes to this file unless you know what you are doing--modify
// the SWIG interface file instead.
//------------------------------------------------------------------------------


public class Attribute : global::System.IDisposable {
  private global::System.Runtime.InteropServices.HandleRef swigCPtr;
  protected bool swigCMemOwn;

  internal Attribute(global::System.IntPtr cPtr, bool cMemoryOwn) {
    swigCMemOwn = cMemoryOwn;
    swigCPtr = new global::System.Runtime.InteropServices.HandleRef(this, cPtr);
  }

  internal static global::System.Runtime.InteropServices.HandleRef getCPtr(Attribute obj) {
    return (obj == null) ? new global::System.Runtime.InteropServices.HandleRef(null, global::System.IntPtr.Zero) : obj.swigCPtr;
  }

  ~Attribute() {
    Dispose();
  }

  public virtual void Dispose() {
    lock(this) {
      if (swigCPtr.Handle != global::System.IntPtr.Zero) {
        if (swigCMemOwn) {
          swigCMemOwn = false;
          crfsuitePINVOKE.delete_Attribute(swigCPtr);
        }
        swigCPtr = new global::System.Runtime.InteropServices.HandleRef(null, global::System.IntPtr.Zero);
      }
      global::System.GC.SuppressFinalize(this);
    }
  }

  public string attr {
    set {
      crfsuitePINVOKE.Attribute_attr_set(swigCPtr, value);
      if (crfsuitePINVOKE.SWIGPendingException.Pending) throw crfsuitePINVOKE.SWIGPendingException.Retrieve();
    } 
    get {
      string ret = crfsuitePINVOKE.Attribute_attr_get(swigCPtr);
      if (crfsuitePINVOKE.SWIGPendingException.Pending) throw crfsuitePINVOKE.SWIGPendingException.Retrieve();
      return ret;
    } 
  }

  public double value {
    set {
      crfsuitePINVOKE.Attribute_value_set(swigCPtr, value);
      if (crfsuitePINVOKE.SWIGPendingException.Pending) throw crfsuitePINVOKE.SWIGPendingException.Retrieve();
    } 
    get {
      double ret = crfsuitePINVOKE.Attribute_value_get(swigCPtr);
      if (crfsuitePINVOKE.SWIGPendingException.Pending) throw crfsuitePINVOKE.SWIGPendingException.Retrieve();
      return ret;
    } 
  }

  public Attribute() : this(crfsuitePINVOKE.new_Attribute__SWIG_0(), true) {
    if (crfsuitePINVOKE.SWIGPendingException.Pending) throw crfsuitePINVOKE.SWIGPendingException.Retrieve();
  }

  public Attribute(string name) : this(crfsuitePINVOKE.new_Attribute__SWIG_1(name), true) {
    if (crfsuitePINVOKE.SWIGPendingException.Pending) throw crfsuitePINVOKE.SWIGPendingException.Retrieve();
  }

  public Attribute(string name, double val) : this(crfsuitePINVOKE.new_Attribute__SWIG_2(name, val), true) {
    if (crfsuitePINVOKE.SWIGPendingException.Pending) throw crfsuitePINVOKE.SWIGPendingException.Retrieve();
  }

}
