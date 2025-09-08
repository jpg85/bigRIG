#pragma once
class Foo
{
public:
    void Method();
    void ConstMethod() const;
    virtual void VirtualMethod();
    virtual void PureVirtualMethod() = 0;
    virtual void ConstPureVirtualMethod() const = 0;
    static void StaticMethod();
private:
    int member;
};