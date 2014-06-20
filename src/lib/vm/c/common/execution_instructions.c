#include "types.h"
#include "debug.h"
#include "execution.h"
#include "jstring.h"
#include "array.h"

// generated at infusion time
#include "jlib_base.h"

ref_t DO_LDS(dj_local_id localStringId) {
	// resolve the string id
	dj_global_id globalStringId = dj_global_id_resolve(dj_exec_getCurrentInfusion(), localStringId);

	dj_object *string = dj_jstring_createFromGlobalId(dj_exec_getVM(), globalStringId);

	if (string==NULL) {
		dj_exec_createAndThrow(BASE_CDEF_java_lang_OutOfMemoryError);
		return 0;
	}

	return VOIDP_TO_REF(string);
}


void DO_INVOKEVIRTUAL(dj_local_id dj_local_id, uint8_t nr_ref_args) {
	// peek the object on the stack
	dj_object *object = REF_TO_VOIDP(dj_exec_stackPeekDeepRef(nr_ref_args));

	// if null, throw exception
	if (object==NULL)
	{
		dj_exec_createAndThrow(BASE_CDEF_java_lang_NullPointerException);
		return;
	}

	// check if the object is still valid
	if (dj_object_getRuntimeId(object)==CHUNKID_INVALID)
	{
		dj_exec_createAndThrow(BASE_CDEF_javax_darjeeling_vm_ClassUnloadedException);
		return;
	}

	dj_global_id resolvedMethodDefId = dj_global_id_resolve(dj_exec_getCurrentInfusion(), dj_local_id);

	DEBUG_LOG(DBG_DARJEELING, ">>>>> invokevirtual METHOD DEF %p.%d\n", resolvedMethodDefId.infusion, resolvedMethodDefId.entity_id);

	// lookup the virtual method
	dj_global_id methodImplId = dj_global_id_lookupVirtualMethod(resolvedMethodDefId, object);

	DEBUG_LOG(DBG_DARJEELING, ">>>>> invokevirtual METHOD IMPL %p.%d\n", methodImplId.infusion, methodImplId.entity_id);

	// check if method not found, and throw an error if this is the case. else, invoke the method
	if (methodImplId.infusion==NULL)
	{
		DEBUG_LOG(DBG_DARJEELING, "methodImplId.infusion is NULL at INVOKEVIRTUAL %p.%d\n", resolvedMethodDefId.infusion, resolvedMethodDefId.entity_id);

		dj_exec_throwHere(dj_vm_createSysLibObject(dj_exec_getVM(), BASE_CDEF_java_lang_VirtualMachineError));
	} else
	{
		callMethod(methodImplId, true);
	}
}

ref_t DO_NEW(dj_local_id dj_local_id) {
	dj_di_pointer classDef;
	dj_global_id dj_global_id = dj_global_id_resolve(dj_exec_getCurrentInfusion(), dj_local_id);

	// get class definition
	classDef = dj_global_id_getClassDefinition(dj_global_id);

	dj_object * object = dj_object_create(
			dj_global_id_getRuntimeClassId(dj_global_id),
			dj_di_classDefinition_getNrRefs(classDef),
			dj_di_classDefinition_getOffsetOfFirstReference(classDef)
			);

	// if create returns null, throw out of memory error
	if (object==NULL) {
		dj_exec_createAndThrow(BASE_CDEF_java_lang_OutOfMemoryError);
		return 0;
	}

	return VOIDP_TO_REF(object);
}

ref_t DO_ANEWARRAY(dj_local_id dj_local_id, uint16_t size) {
	dj_global_id dj_global_id = dj_global_id_resolve(dj_exec_getCurrentInfusion(), dj_local_id);
	uint16_t id = dj_global_id_getRuntimeClassId(dj_global_id);
	dj_ref_array *arr = dj_ref_array_create(id, size);

	if (arr==nullref) {
		dj_exec_createAndThrow(BASE_CDEF_java_lang_OutOfMemoryError);
		return 0;
	}
	else
		return VOIDP_TO_REF(arr);
}
