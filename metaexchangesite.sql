-- phpMyAdmin SQL Dump
-- version 3.4.9
-- http://www.phpmyadmin.net
--
-- Host: localhost
-- Generation Time: Feb 03, 2015 at 06:37 PM
-- Server version: 5.1.61
-- PHP Version: 5.3.3

SET SQL_MODE="NO_AUTO_VALUE_ON_ZERO";
SET time_zone = "+00:00";


/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!40101 SET NAMES utf8 */;

--
-- Database: `metaexchangesite`
--

-- --------------------------------------------------------

--
-- Table structure for table `stats`
--

CREATE TABLE IF NOT EXISTS `stats` (
  `bid_price` decimal(18,8) NOT NULL,
  `ask_price` decimal(18,8) NOT NULL,
  `max_btc` decimal(18,8) NOT NULL,
  `max_bitassets` decimal(18,8) NOT NULL,
  `last_update` datetime NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=latin1;

-- --------------------------------------------------------

--
-- Table structure for table `transactions`
--

CREATE TABLE IF NOT EXISTS `transactions` (
  `received_txid` varchar(64) NOT NULL,
  `sent_txid` varchar(64) DEFAULT NULL,
  `asset` varchar(8) NOT NULL,
  `amount` decimal(18,8) NOT NULL DEFAULT '0.00000000',
  `date` datetime NOT NULL,
  `type` enum('bitcoinDeposit','bitsharesDeposit','bitcoinRefund','bitsharesRefund','none') NOT NULL,
  `notes` varchar(64) DEFAULT NULL,
  PRIMARY KEY (`received_txid`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1;

/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
