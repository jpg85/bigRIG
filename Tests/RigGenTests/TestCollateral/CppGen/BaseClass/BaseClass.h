#pragma once
class BaseA
{
};
class BaseB
{
};
class Derived : public BaseA, protected virtual BaseB
{
};