#pragma once
#include <cstdint>
#include <functional>
#include <memory>
#include <variant>

enum class MyEnum : std::int64_t
{
    a,
    b = 123,
    c,
    d = 456
};

typedef int IntTypedef;
using FloatTypedef = float;

struct VariousTypes
{
    IntTypedef myInt;
    FloatTypedef myFloat;
    std::int16_t c;
    char myChar;
    std::variant<int, float> myVariant;
    std::shared_ptr<VariousTypes> mySharedPtr;
    std::function<void(int)> myFunction;
    MyEnum myEnum;
    double myDouble;
    bool myBool;
    long myLong;
};