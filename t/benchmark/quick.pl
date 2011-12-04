#!/usr/bin/perl -w

use t::lib::TestBenchmark;

my $bench = Dumbbench->new( initial_runs => 10 );

run_bytecode( $bench, 't/benchmark/tests/rec.pl.pb' );
run_bytecode( $bench, 't/benchmark/tests/iter.pl.pb' );

$bench->run;
$bench->report;
