#pragma once
using namespace System;
namespace CLI {

    template<class T>
    public ref class ManagedObject
    {
    protected:
        T* m_Instance;
    public:
        ManagedObject(T* instance)
            : m_Instance(instance)
        {
        }
        virtual ~ManagedObject()
        {
            if (m_Instance != nullptr)
            {
                delete m_Instance;
            }
        }
        !ManagedObject()
        {
            if (m_Instance != nullptr)
            {
                delete m_Instance;
            }
        }
        T* GetInstance()
        {
            return m_Instance;
        }
    };
}