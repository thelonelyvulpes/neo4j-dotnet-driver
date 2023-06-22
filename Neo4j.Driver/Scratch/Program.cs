// See https://aka.ms/new-console-template for more information

using System.Linq.Expressions;
using AutoMapper;

// Expression<Func<Test, int>> expr = t => t.Id;
// Get which property is being accessed
// var memberExpr = (MemberExpression)expr.Body;
// Console.WriteLine(memberExpr.Member.Name);
// Console.WriteLine(memberExpr.Type);
// Results in a Runtime error if the property is not public
// Console.WriteLine(memberExpr
//     .Member
//     .DeclaringType
//     .GetProperties()
//     .Single(x => x.Name == memberExpr.Member.Name)
//     .SetMethod
//     .IsPublic);

// Create Automapper mapping for Test and TestDto
var config = new MapperConfiguration(cfg =>
{
    cfg.CreateMap<TestDto, Test>()
        .ForMember(x => x.GetOnly, x => x.MapFrom(y => y.GetOnly))
        .ForMember(x => x.Id, x => x.MapFrom(y => y.Id));
});

var mapper = new Mapper(config);
var source = new TestDto { Id = 1, GetOnly = 2 };
var value = mapper.Map<Test>(source);

Console.WriteLine($"value: {value.Id} {value.GetOnly}");

class Test
{
    public int Id { get; set; }
    public int GetOnly { get; }
}

class TestDto
{
    public int Id { get; set; }
    public int GetOnly { get; set; }
}