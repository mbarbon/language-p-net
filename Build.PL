#!/usr/bin/perl -w

use strict;
use warnings;
use lib 'inc';

use My::Build;

my $build = My::Build->new
  ( module_name          => 'Language::P::Net',
    license              => 'perl',
    requires             =>
        { 'Module::Build'     => '0.28',
          'Language::P'       => '0.01_04',
          'TAP::Harness'      => '3.10',
          'perl'              => '5.8.3',
          },
    get_options          =>
        { perl5          =>
              { type     => '=s',
                },
          dlr            =>
              { type     => '=s',
                },
          p              =>
              { type     => '=s',
                },
          configuration  =>
              { type     => '=s',
                },
          },
    );
$build->create_build_script;
