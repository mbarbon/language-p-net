#!/usr/bin/perl -w

use t::lib::TestBenchmark;

my $bench = Dumbbench->new( initial_runs => 10 );

run_bytecode( $bench, 't/benchmark/tests/rec.pl.pb' );
run_bytecode( $bench, 't/benchmark/tests/iter.pl.pb' );
run_parse( $bench, 't/benchmark/tests/iter.pl' );

$bench->run;
$bench->report;
