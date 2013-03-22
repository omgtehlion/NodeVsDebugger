////////////////////////////////////
// some comments
//

var f1 = function() { };

console.log("hello");

var π = Math.PI;
var ಠ_ಠ = eval;

var Class1 = function() { }
Class1.prototype.a = 10;
Class1.prototype.b = function() { return 5; }

var ಠ_ಠ = {
    one: 1,
    arr: ["a", "b", "c"],
    emptyArr: [],
    str: "aaaaaaaaaaaaa".replace(/a/g, "aaaaaaaaaaaaa"),
    rx: /asdasd/gi,
    fl: 1.555,
    omg: Infinity,
    n: null,
    u: undefined,
    f1: f1,
    c1: new Class1(),
    "$#&#@$& ": "asdasd"
};
ಠ_ಠ.__defineGetter__('g', function() { return 'g'; });
ಠ_ಠ.__defineSetter__('g', function() { });

console.log(", World!");

var included = require("./included");
included.onload();

var x = function(cnt) {
    debugger;
    for (i = 0; i < cnt; i++) {
        var j = "тест юникода " + i;
        included.print(j)
    }
    throw "asdadasd";
}
x(5);
console.log("Bye!");
